using Constants;
using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    public partial struct ShootSystem : ISystem
    {
        private float _nextShotAt;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<InputComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var shootState = SystemAPI.GetSingleton<InputComponent>().Shoot;

            if (shootState.ReleasedThisFrame)
            {
                _nextShotAt = float.NegativeInfinity;
                return;
            }

            var shouldShoot = shootState.CurrentlyPressed && _nextShotAt < time;
            if (!shouldShoot) return;
            _nextShotAt = time + 0.2f;

            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var player = SystemAPI.GetComponent<Player>(playerEntity);
            var climbKnots = SystemAPI.GetBuffer<ClimbKnot>(playerEntity);
            var rotateKnots = SystemAPI.GetBuffer<RotateKnot>(playerEntity);

            var bullet = state.EntityManager.Instantiate(player.BulletPrefab);
            var localTransform = CalculateLocalTransform(time, climbKnots);
            Rotate(time, rotateKnots, ref localTransform);

            var forward = math.forward(localTransform.Rotation);
            localTransform.Position += forward * 0.65f;

            SystemAPI.SetComponent(bullet, localTransform);
            SystemAPI.SetComponent(
                bullet,
                new Bullet
                {
                    Forward = forward,
                    Start = localTransform.Position,
                    StartTime = time,
                }
            );
        }

        private LocalTransform CalculateLocalTransform(float time, DynamicBuffer<ClimbKnot> knots)
        {
            if (knots.Length == 1)
            {
                return new LocalTransform
                {
                    Position = knots[0].Position,
                    Rotation = knots[0].Rotation,
                    Scale = 0.1f,
                };
            }

            var normalizedTime = math.unlerp(knots[0].Time, knots[1].Time, time);
            var lerpTime = normalizedTime;
            var slerpTime = normalizedTime;

            var jumpFlags = ClimbFlags.Jump | ClimbFlags.JumpForward;
            if ((knots[1].Flags & jumpFlags) != ClimbFlags.None)
            {
                lerpTime = math.cos((1 - normalizedTime) * math.PIHALF);
                slerpTime = math.cos(normalizedTime * math.PIHALF);
            }
            else if ((knots[1].Flags & ClimbFlags.FallForward) != ClimbFlags.None)
            {
                lerpTime = 1 - math.cos(normalizedTime * math.PIHALF);
                slerpTime = 1 - math.cos((1 - normalizedTime) * math.PIHALF);
            }

            return new LocalTransform
            {
                Position = math.lerp(knots[0].Position, knots[1].Position, lerpTime),
                Rotation = math.slerp(knots[0].Rotation, knots[1].Rotation, slerpTime),
                Scale = 0.1f,
            };
        }

        private void Rotate(float time, DynamicBuffer<RotateKnot> knot, ref LocalTransform localTransformRw)
        {
            if (knot.Length == 1)
            {
                localTransformRw.Rotation = math.mul(localTransformRw.Rotation, knot[0].Rotation);
                return;
            }

            var normalizedTime = math.unlerp(knot[0].Time, knot[1].Time, time);
            var rotation = math.slerp(knot[0].Rotation, knot[1].Rotation, normalizedTime);
            localTransformRw.Rotation = math.mul(localTransformRw.Rotation, rotation);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}