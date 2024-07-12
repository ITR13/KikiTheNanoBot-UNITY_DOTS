using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    public partial struct GearRenderSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            state.Dependency = new UpdateGearSpeeds
            {
                DeltaTime = deltaTime,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct UpdateGearSpeeds : IJobEntity
        {
            private const float Acceleration = 10f;
            public float DeltaTime;

            private void Execute(
                in Gear gear,
                [ReadOnly] in DynamicBuffer<MultiPosition> positions,
                ref GearSpeed speed
            )
            {
                var pos = positions[^1].Position;
                var targetSpeed = !gear.Powered ? 0 : ((pos.x ^ pos.z) & 1) * 2 - 1;
                speed.Speed = targetSpeed < speed.Speed
                    ? math.max(targetSpeed, speed.Speed - DeltaTime * Acceleration)
                    : math.min(targetSpeed, speed.Speed + DeltaTime * Acceleration);
            }
        }
    }
}