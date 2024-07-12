using Data;
using Enums;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(RenderSystemGroup))]
partial struct KnotRenderPosSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.Time;
        var elapsedTime = (float)time.ElapsedTime;

        var jumpFlags = ClimbFlags.Jump | ClimbFlags.JumpForward;

        foreach (
            var (localTransform, knot)
            in SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<ClimbKnot>>())
        {
            ref var localTransformRw = ref localTransform.ValueRW;
            if (knot.Length == 1)
            {
                localTransformRw.Position = knot[0].Position;
                localTransformRw.Rotation = knot[0].Rotation;
                continue;
            }

            var normalizedTime = math.unlerp(knot[0].Time, knot[1].Time, elapsedTime);
            var lerpTime = normalizedTime;
            var slerpTime = normalizedTime;

            if ((knot[1].Flags & jumpFlags) != ClimbFlags.None)
            {
                lerpTime = math.cos((1 - normalizedTime) * math.PIHALF);
                slerpTime = math.cos(normalizedTime * math.PIHALF);
            }
            else if ((knot[1].Flags & ClimbFlags.FallForward) != ClimbFlags.None)
            {
                // lerpTime = math.cos(normalizedTime * math.PIHALF);
                // slerpTime = math.cos((1 - normalizedTime) * math.PIHALF);
            }

            localTransformRw.Position = math.lerp(knot[0].Position, knot[1].Position, lerpTime);
            localTransformRw.Rotation = math.slerp(knot[0].Rotation, knot[1].Rotation, slerpTime);
        }

        foreach (
            var (localTransform, knot, entity)
            in SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<RotateKnot>>().WithEntityAccess())
        {
            ref var localTransformRw = ref localTransform.ValueRW;
            if (knot.Length == 1)
            {
                localTransformRw.Rotation = math.mul(localTransformRw.Rotation, knot[0].Rotation);
                continue;
            }

            var normalizedTime = math.unlerp(knot[0].Time, knot[1].Time, elapsedTime);
            var rotation = math.slerp(knot[0].Rotation, knot[1].Rotation, normalizedTime);
            localTransformRw.Rotation = math.mul(localTransformRw.Rotation, rotation);
        }
    }
}