using Constants;
using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Data.BoundUtils;

namespace Logic
{
    public partial struct GearAndWireSystem : ISystem
    {
        private ComponentType MultiPosition;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellHolder>();
            MultiPosition = typeof(MultiPosition);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            var changeQuery = SystemAPI.QueryBuilder()
                .WithAll<MultiPosition>()
                .WithAny<Gear, WireCube, MotorTag>()
                .Build();
            changeQuery.SetChangedVersionFilter(MultiPosition);

            if (changeQuery.IsEmpty) return;

            ref var cells = ref SystemAPI.GetSingletonRW<CellHolder>().ValueRW;
            var bounds = cells.RoomBounds;

            // Generate map over Gears and WireCubes
            var gearMap = new NativeArray<RefRW<Gear>>(cells.Volume, Allocator.Temp);
            var wireCubeMap = new NativeArray<RefRW<WireCube>>(cells.Volume, Allocator.Temp);
            {
                foreach (
                    var (positions, gear)
                    in SystemAPI.Query<DynamicBuffer<MultiPosition>, RefRW<Gear>>())
                {
                    if (positions.Length > 1) continue;
                    var position = positions[0].Position;
                    var index = PositionToIndex(bounds, position);

                    gearMap[index] = gear;
                    gear.ValueRW.Powered = false;
                }
            }

            {
                foreach (
                    var (positions, wireCube)
                    in SystemAPI.Query<DynamicBuffer<MultiPosition>, RefRW<WireCube>>())
                {
                    if (positions.Length > 1) continue;
                    var position = positions[0].Position;
                    var index = PositionToIndex(bounds, position);

                    wireCubeMap[index] = wireCube;
                    wireCube.ValueRW.Powered = false;
                }
            }

            // In hindsight, should've just made this a readonly span
            var surroundingArray = new NativeArray<int3>(4, Allocator.Temp);
            surroundingArray[0] = new int3(1, 0, 0);
            surroundingArray[1] = new int3(-1, 0, 0);
            surroundingArray[2] = new int3(0, 0, 1);
            surroundingArray[3] = new int3(0, 0, -1);

            // Propagate gear power
            foreach (var startPositions in SystemAPI.Query<DynamicBuffer<MultiPosition>>().WithAll<MotorTag>())
            {
                if (startPositions.Length > 1) continue;
                var startPosition = startPositions[0].Position;
                ThrowIfOutOfBounds(bounds, startPosition);

                var queue = new NativeQueue<int3>(Allocator.Temp);
                queue.Enqueue(startPosition);

                var startIndex = PositionToIndex(bounds, startPosition);
                if (gearMap[startIndex].IsValid) gearMap[startIndex].ValueRW.Powered = true;

                while (queue.TryDequeue(out var position))
                {
                    ThrowIfOutOfBounds(bounds, position);

                    foreach (var delta in surroundingArray)
                    {
                        var otherPos = position + delta;
                        if (IsOutOfBounds(bounds, otherPos)) continue;

                        var otherIndex = PositionToIndex(bounds, otherPos);
                        var otherGear = gearMap[otherIndex];
                        if (!otherGear.IsValid || otherGear.ValueRO.Powered) continue;
                        otherGear.ValueRW.Powered = true;
                        queue.Enqueue(otherPos);
                    }
                }
            }

            // Power wires from gears
            cells.PoweredGroups.Clear();
            var wireCubeQueue = new NativeQueue<int3>(Allocator.Temp);
            var poweredWire = false;

            foreach (
                var (positions, gear)
                in SystemAPI.Query<DynamicBuffer<MultiPosition>, Gear>().WithAll<GearToWireTag>())
            {
                if (!gear.Powered) continue;
                var position = positions[^1].Position;
                cells.GetWire(position, out var direction, out var group);

                // Check if the gear is powering a wire directly
                var hasWire = (direction & Direction.Down) != Direction.None && group >= 0;
                if (hasWire)
                {
                    poweredWire = true;
                    cells.PoweredGroups.Set(group, true);
                }

                // Check if the gear is powering a wire cube
                position.y -= 1;
                var index = PositionToIndex(bounds, position);
                if (position.y <= 0 || !wireCubeMap[index].IsValid) continue;
                wireCubeMap[index].ValueRW.Powered = true;
                wireCubeQueue.Enqueue(position);
            }

            // TODO: Swap this full wire propagation later
            // Propagate Wire power
            do
            {
                // Iterate newly powered cubes
                while (wireCubeQueue.TryDequeue(out var position))
                    for (var i = 0; i < 6; i++)
                    {
                        var direction = DirectionUtils.IndexToDirection(i);
                        var (vector, _) = DirectionUtils.IndexToVectorAndCounterpart(i);
                        var surroundingPosition = position + vector;

                        if (IsOutOfBounds(bounds, surroundingPosition)) continue;

                        // Check if the cube is powering a wire directly
                        cells.GetWire(surroundingPosition, out var wireDirections, out var group);
                        if (group >= 0 &&
                            !cells.PoweredGroups.IsSet(group) &&
                            (wireDirections & (Direction)(~(int)direction)) != Direction.None)
                        {
                            cells.PoweredGroups.Set(group, true);
                            poweredWire = true;
                        }

                        // Check if cube is powering another cube
                        var surroundingIndex = PositionToIndex(bounds, surroundingPosition);
                        if (!wireCubeMap[surroundingIndex].IsValid || wireCubeMap[surroundingIndex].ValueRO.Powered)
                            continue;
                        wireCubeMap[surroundingIndex].ValueRW.Powered = true;
                        wireCubeQueue.Enqueue(surroundingPosition);
                    }

                // TODO: Also check for diagonal cubes
                if (!poweredWire) break;
                poweredWire = false;
                foreach (
                    var (positions, wireCube)
                    in SystemAPI.Query<DynamicBuffer<MultiPosition>, RefRW<WireCube>>())
                {
                    if (positions.Length > 1 || wireCube.ValueRO.Powered) continue;
                    var position = positions[^1].Position;

                    cells.GetWire(position, out var _, out var group);
                    if (
                        group < 0 ||
                        !cells.PoweredGroups.IsSet(group)
                    )
                        continue;
                    // NB: Queue the cube, not the wire position
                    var index = PositionToIndex(bounds, position);
                    wireCubeMap[index].ValueRW.Powered = true;
                    wireCubeQueue.Enqueue(position);
                }
            } while (!wireCubeQueue.IsEmpty());
        }
    }
}