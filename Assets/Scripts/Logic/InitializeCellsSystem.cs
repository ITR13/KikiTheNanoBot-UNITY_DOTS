using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
// [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
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

        var solidQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld>().Build();
        var solidTransforms = solidQuery.ToComponentDataArray<LocalToWorld>(state.WorldUpdateAllocator);
        var solidPositions = LtwToNativeArray(ref state, solidTransforms);

        var pushableQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld, PushableTag>().Build();
        var pushableTransforms = pushableQuery.ToComponentDataArray<LocalToWorld>(state.WorldUpdateAllocator);
        var pushableEntities = pushableQuery.ToEntityArray(state.WorldUpdateAllocator);
        var pushablePositions = LtwToNativeArray(ref state, pushableTransforms);

        foreach (var (room, entity) in SystemAPI.Query<Room>().WithAbsent<CellHolder>().WithEntityAccess())
        {
            var roomVolume = room.Bounds.x * room.Bounds.y * room.Bounds.z;
            var pushable = new NativeArray<Entity>(roomVolume, Allocator.Persistent);
            var solid = new NativeBitArray(roomVolume, Allocator.Persistent);

            var cellHolder =
                new CellHolder
                {
                    RoomBounds = room.Bounds,
                    PushablePositions = pushable,
                    SolidPositions = solid,
                };

            foreach (var solidPosition in solidPositions)
            {
                cellHolder.SetSolid(solidPosition, true);
            }

            for (var i = 0; i < pushablePositions.Length; i++)
            {
                cellHolder.SetPushable(pushablePositions[i], pushableEntities[i]);
            }

            ecb.AddComponent(entity, cellHolder);
        }
    }

    private NativeArray<int3> LtwToNativeArray(ref SystemState state, NativeArray<LocalToWorld> locals)
    {
        var positions = CollectionHelper.CreateNativeArray<int3>(
            locals.Length,
            state.WorldUpdateAllocator
        );
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = (int3)math.round(locals[i].Position);
        }

        return positions;
    }
}