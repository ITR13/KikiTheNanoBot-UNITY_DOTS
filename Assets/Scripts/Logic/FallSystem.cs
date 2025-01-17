using System;
using Constants;
using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Logic
{
    [BurstCompile]
    [UpdateInGroup(typeof(ControlSystemGroup))]
    internal partial struct FallSystem : ISystem
    {
        private struct Faller : IComparable<Faller>
        {
            public float Time;
            public Fall Fall;
            public EnabledRefRW<Fall> EnabledRef;
            public DynamicBuffer<ClimbKnot> ClimbKnots;
            public DynamicBuffer<MultiPosition> MultiPositions;
            public Entity Entity;

            public int CompareTo(Faller other)
            {
                return Time.CompareTo(other.Time);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<CellHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            ref var cells = ref SystemAPI.GetSingletonRW<CellHolder>().ValueRW;
            var time = (float)SystemAPI.Time.ElapsedTime;

            var fallers = new NativeList<Faller>(5, state.WorldUpdateAllocator);

            foreach (
                var (fall, fallEnabled, climbKnots, multiPositions, entity) in SystemAPI
                    .Query<Fall, EnabledRefRW<Fall>, DynamicBuffer<ClimbKnot>, DynamicBuffer<MultiPosition>>()
                    .WithEntityAccess())
            {
                var lastKnot = climbKnots[^1];
                if (lastKnot.Time - StructConstants.CoyoteTime >= time) return;

                fallers.Add(
                    new Faller
                    {
                        Time = math.max(lastKnot.Time, time),
                        Fall = fall,
                        EnabledRef = fallEnabled,
                        ClimbKnots = climbKnots,
                        MultiPositions = multiPositions,
                        Entity = entity,
                    }
                );
            }

            fallers.Sort();

            foreach (var faller in fallers)
            {
                var currentPosition = faller.MultiPositions[^1].Position;
                var nextPosition = currentPosition + faller.Fall.Direction;

                if (cells.IsSolid(nextPosition))
                {
                    faller.EnabledRef.ValueRW = false;
                    if (SystemAPI.HasComponent<OneShotAudioReference>(faller.Entity))
                    {
                        var reference = SystemAPI.GetComponent<OneShotAudioReference>(faller.Entity);
                        var audio = ecb.Instantiate(reference.Entity);
                        ecb.SetComponent(
                            audio,
                            new LocalToWorld
                            {
                                Value = float4x4.Translate(currentPosition),
                            }
                        );
                    }

                    continue;
                }

                var endTime = faller.Time + faller.Fall.Duration;

                // ReSharper disable twice PossiblyImpureMethodCallOnReadonlyVariable
                faller.ClimbKnots.Add(
                    new ClimbKnot
                    {
                        Position = nextPosition,
                        Rotation = faller.ClimbKnots[^1].Rotation,
                        Time = endTime,
                        Flags = ClimbFlags.Fall,
                    }
                );
                faller.MultiPositions.Add(
                    new MultiPosition
                    {
                        Position = nextPosition,
                        Time = endTime,
                        Flags = ClimbFlags.Fall,
                    }
                );

                cells.SetSolid(nextPosition, true);
                cells.SetPushable(nextPosition, faller.Entity);
            }
        }
    }
}