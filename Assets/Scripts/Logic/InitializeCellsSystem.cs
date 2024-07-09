using Data;
using Enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
partial struct InitializeCellsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
    }

    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI
            .GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var solidQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, SolidTag>().Build();
        var solidTransforms = solidQuery.ToComponentDataArray<LocalToWorld>(state.WorldUpdateAllocator);
        var solidPositions = LtwToNativeArray(ref state, solidTransforms);

        var pushableQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, PushableTag>().Build();
        var pushableTransforms = pushableQuery.ToComponentDataArray<LocalToWorld>(state.WorldUpdateAllocator);
        var pushableEntities = pushableQuery.ToEntityArray(state.WorldUpdateAllocator);
        var pushablePositions = LtwToNativeArray(ref state, pushableTransforms);

        var wireQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, WireTag>().Build();
        var wireTransforms = wireQuery.ToComponentDataArray<LocalToWorld>(state.WorldUpdateAllocator);
        var (wirePositions, wireDirections) = LtwToWires(ref state, wireTransforms);

        foreach (var (room, entity) in SystemAPI.Query<Room>().WithAbsent<CellHolder>().WithEntityAccess())
        {
            var roomVolume = room.Bounds.x * room.Bounds.y * room.Bounds.z;
            var pushable = new NativeArray<Entity>(roomVolume, Allocator.Persistent);
            var solid = new NativeBitArray(roomVolume, Allocator.Persistent);

            var wires = new NativeArray<Direction>(roomVolume, Allocator.Persistent);
            var wireGroups = new NativeArray<int>(
                roomVolume,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory
            );
            for (var i = 0; i < wireGroups.Length; i++)
            {
                wireGroups[i] = -1;
            }

            var cellHolder =
                new CellHolder
                {
                    RoomBounds = room.Bounds,
                    PushablePositions = pushable,
                    SolidPositions = solid,
                    Wires = wires,
                    WiresGroup = wireGroups,
                };

            foreach (var solidPosition in solidPositions)
            {
                cellHolder.SetSolid(solidPosition, true);
            }

            for (var i = 0; i < pushablePositions.Length; i++)
            {
                cellHolder.SetPushable(pushablePositions[i], pushableEntities[i]);
            }

            for (var i = 0; i < wirePositions.Length; i++)
            {
                CellHolderExt.ThrowIfOutOfBounds(cellHolder.RoomBounds, wirePositions[i]);
                var index = CellHolderExt.GetIndex(cellHolder.RoomBounds, wirePositions[i]);
                wires[index] |= wireDirections[i];
            }

            var groupCount = 0;
            for (var i = 0; i < wirePositions.Length; i++)
            {
                WireDfs(ref cellHolder, ref groupCount, wirePositions[i]);
            }

            ecb.AddComponent(entity, cellHolder);
        }
    }

    private NativeArray<int3> LtwToNativeArray(ref SystemState state, NativeArray<LocalToWorld> locals)
    {
        var positions = CollectionHelper.CreateNativeArray<int3>(
            locals.Length,
            state.WorldUpdateAllocator,
            NativeArrayOptions.UninitializedMemory
        );
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = (int3)math.round(locals[i].Position);
        }

        return positions;
    }

    private (NativeArray<int3>, NativeArray<Direction>) LtwToWires(
        ref SystemState state,
        NativeArray<LocalToWorld> locals
    )
    {
        var positions = CollectionHelper.CreateNativeArray<int3>(
            locals.Length,
            state.WorldUpdateAllocator,
            NativeArrayOptions.UninitializedMemory
        );
        var forwards = CollectionHelper.CreateNativeArray<float3>(
            locals.Length,
            state.WorldUpdateAllocator,
            NativeArrayOptions.UninitializedMemory
        );
        var directions = CollectionHelper.CreateNativeArray<Direction>(
            locals.Length,
            state.WorldUpdateAllocator,
            NativeArrayOptions.UninitializedMemory
        );
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = (int3)math.round(locals[i].Position);
        }

        for (var i = 0; i < positions.Length; i++)
        {
            forwards[i] = math.forward(locals[i].Rotation);
        }

        
        for (var i = 0; i < forwards.Length; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                if (math.abs(forwards[i][j]) < 0.5f) continue;
                var k = forwards[i][j] < 0 ? 0 : 3;
               
                directions[i] = (Direction)(1 << (j + k));
                goto positions;
            }

            directions[i] = Direction.None;
            positions: ;
        }

        return (positions, directions);
    }

    private void WireDfs(ref CellHolder holder, ref int nextGroup, int3 startPosition)
    {
        holder.GetWire(startPosition, out _, out var group);
        if (group >= 0) return;

        var queue = new NativeQueue<int3>(Allocator.Temp);
        queue.Enqueue(startPosition);

        group = nextGroup++;

        while (queue.TryDequeue(out var position))
        {
            var index = CellHolderExt.GetIndex(holder.RoomBounds, position);

            if (holder.WiresGroup[index] != -1) continue;
            holder.WiresGroup[index] = group;
            var directions = holder.Wires[index];

            for (var i = 0; i < 6; i++)
            {
                var direction = DirectionUtils.IndexToDirection(i);
                if ((directions & direction) == Direction.None) continue;

                var (vector, counterPart) = DirectionUtils.IndexToVectorAndCounterpart(i);

                var surrounding = DirectionUtils.VectorToSurrounding(vector);

                var belowDirectionsNeeded = new[]
                {
                    (Direction)(1 << ((counterPart + 1) % 6)),
                    (Direction)(1 << ((counterPart + 4) % 6)),
                    (Direction)(1 << ((counterPart + 5) % 6)),
                    (Direction)(1 << ((counterPart + 2) % 6)),
                };

                for (var j = 0; j < surrounding.Length; j++)
                {
                    var otherPosition = surrounding[j] + position;
                    if (!CellHolderExt.IsOutOfBounds(holder.RoomBounds, otherPosition))
                    {
                        holder.GetWire(otherPosition, out var otherDirection, out var otherGroup);
                        if (otherGroup == -1 && (otherDirection & direction) != Direction.None)
                        {
                            queue.Enqueue(otherPosition);
                        }
                    }


                    otherPosition += vector;
                    if (!CellHolderExt.IsOutOfBounds(holder.RoomBounds, otherPosition))
                    {
                        holder.GetWire(otherPosition, out var otherDirection, out var otherGroup);
                        if (otherGroup == -1 && (otherDirection & belowDirectionsNeeded[j]) != Direction.None)
                        {
                            queue.Enqueue(otherPosition);
                        }
                    }
                }
            }
        }
    }
}