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
    public partial struct GearRenderSystem : ISystem
    {
        private const float Teeth = 8;
        private const float GearSpeed = 0.5f;
        private const float CatchupSpeed = 1.2f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = SystemAPI.Time;
            var deltaTime = time.DeltaTime;
            var targetRotation = math.frac((float)time.ElapsedTime * GearSpeed * Teeth);

            state.Dependency = new UpdateTargetRotation
            {
                TargetRotationNormalized = targetRotation,
                DeltaTime = deltaTime,
            }.Schedule(state.Dependency);
            state.Dependency = new UpdateActualRotation
            {
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct UpdateTargetRotation : IJobEntity
        {
            public float TargetRotationNormalized;
            public float DeltaTime;

            private void Execute(
                in Gear gear,
                [ReadOnly] in DynamicBuffer<MultiPosition> positions,
                ref GearRotation rotation
            )
            {
                if (!gear.Powered)
                {
                    return;
                }

                var pos = positions[^1].Position;
                var targetMultiplier = (pos.x ^ pos.z) & 1;
                var target = TargetRotationNormalized * targetMultiplier + (1 - TargetRotationNormalized) * (1 - targetMultiplier);

                var delta = target - rotation.Normalized;
                var absDelta = math.abs(delta);

                var maxDelta = DeltaTime * GearSpeed * CatchupSpeed * Teeth;

                if (absDelta <= maxDelta)
                {
                    rotation.Normalized = target;
                }
                else if (absDelta >= 1 - maxDelta)
                {
                    rotation.Normalized = target;
                    rotation.Rotations += math.sign(delta);
                }
                else
                {
                    var deltaMultiplier = 1 - math.round(absDelta) * 2;
                    var speed = math.sign(delta) * maxDelta * deltaMultiplier;
                    rotation.Normalized += speed;

                    if (rotation.Normalized >= 1)
                    {
                        rotation.Normalized -= 1;
                        rotation.Rotations += 1;
                    }
                    else if (rotation.Normalized < 0)
                    {
                        rotation.Normalized += 1;
                        rotation.Rotations -= 1;
                    }
                }
            }
        }

        [BurstCompile]
        public partial struct UpdateActualRotation : IJobEntity
        {
            public void Execute(in GearRotation gearRotation, ref LocalTransform localToWorld)
            {
                localToWorld.Rotation = quaternion.Euler(
                    0,
                    (gearRotation.Normalized + gearRotation.Rotations) * math.PI * 2 / Teeth,
                    0
                );
            }
        }
    }
}