using Data;
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

        foreach (
            var (localTransform, knot, entity)
            in SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<ClimbKnot>>().WithEntityAccess())
        {

            ref var localTransformRw = ref localTransform.ValueRW;
            if (knot.Length == 1)
            {
                localTransformRw.Position = knot[0].Position;
                localTransformRw.Rotation = knot[0].Rotation;
                continue;
            }

            var normalizedTime = math.unlerp(knot[0].Time, knot[1].Time, elapsedTime);

            localTransformRw.Position = math.lerp(knot[0].Position, knot[1].Position, normalizedTime);
            localTransformRw.Rotation = math.slerp(knot[0].Rotation, knot[1].Rotation, normalizedTime);
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