using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [UpdateAfter(typeof(KnotRenderPosSystem))]
    public partial struct WheelRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<LocalTransform, Wheel, Parent>().Build();

            var wheelKnotLookup = SystemAPI.GetBufferLookup<WheelKnot>(true);
            var time = SystemAPI.Time;

            state.Dependency = new WheelRenderSystemJob
            {
                WheelKnotLookup = wheelKnotLookup,
                ElapsedTime = (float)time.ElapsedTime,
            }.Schedule(query, state.Dependency);
        }

        [BurstCompile]
        public partial struct WheelRenderSystemJob : IJobEntity
        {
            [ReadOnly] public BufferLookup<WheelKnot> WheelKnotLookup;

            public float ElapsedTime;

            public void Execute(ref LocalTransform transform, in Parent parent, in Wheel wheel)
            {
                var knots = WheelKnotLookup[parent.Value];

                var knotRadians = wheel.IsLeftWheel ? knots[0].LeftRotation : knots[0].RightRotation;
                if (knots.Length > 1)
                {
                    var normalizedTime = math.unlerp(knots[0].Time, knots[1].Time, ElapsedTime);
                    var nextKnotRadians =  wheel.IsLeftWheel ? knots[1].LeftRotation : knots[1].RightRotation;
                    knotRadians = math.lerp(knotRadians, nextKnotRadians, normalizedTime);
                }

                transform.Rotation = quaternion.Euler(
                    0f,
                    math.PIHALF,
                    knotRadians
                );
            }
        }
    }
}