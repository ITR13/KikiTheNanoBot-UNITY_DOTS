using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(RenderSystemGroup))]
partial struct RunMotors : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.Time;
        var elapsedTime = (float)time.ElapsedTime;

        foreach (
            var (localTransform, knot, entity)
            in SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<ClimbKnot>>().WithEntityAccess())
        {
            if (knot.IsEmpty) continue;

            var index = 1;
            while (index < knot.Length &&
                   (knot[index].Time < elapsedTime ||
                    knot[index - 1].Time >= knot[index].Time))
            {
                index++;
            }

            index--;
            if (index > 0)
            {
                knot.RemoveRangeSwapBack(0, index);
                index = 0;
            }

            ref var localTransformRw = ref localTransform.ValueRW;
            if (index == knot.Length - 1)
            {
                localTransformRw.Position = knot[index].Position;
                localTransformRw.Rotation = knot[index].Rotation;
                continue;
            }

            var normalizedTime = math.unlerp(knot[index].Time, knot[index + 1].Time, elapsedTime);

            localTransformRw.Position = math.lerp(knot[index].Position, knot[index + 1].Position, normalizedTime);
            localTransformRw.Rotation = math.slerp(knot[index].Rotation, knot[index + 1].Rotation, normalizedTime);
        }

        foreach (
            var (localTransform, knot, entity)
            in SystemAPI.Query<RefRW<LocalTransform>, DynamicBuffer<RotateKnot>>().WithEntityAccess())
        {
            if (knot.IsEmpty) continue;

            var index = 1;
            while (index < knot.Length &&
                   (knot[index].Time < elapsedTime ||
                    knot[index - 1].Time >= knot[index].Time))
            {
                index++;
            }

            index--;
            if (index > 0)
            {
                knot.RemoveRangeSwapBack(0, index);
                index = 0;
            }

            ref var localTransformRw = ref localTransform.ValueRW;
            if (index == knot.Length - 1)
            {
                localTransformRw.Rotation = math.mul(localTransformRw.Rotation, knot[index].Rotation);
                continue;
            }

            var normalizedTime = math.unlerp(knot[index].Time, knot[index + 1].Time, elapsedTime);
            var rotation = math.slerp(knot[index].Rotation, knot[index + 1].Rotation, normalizedTime);
            localTransformRw.Rotation = math.mul(localTransformRw.Rotation, rotation);
        }
    }
}