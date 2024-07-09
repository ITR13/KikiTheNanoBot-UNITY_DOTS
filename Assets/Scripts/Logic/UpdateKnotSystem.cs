using Data;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(ControlSystemGroup))]
[UpdateBefore(typeof(PlayerControllerSystem))]
partial struct UpdateKnotSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CellHolder>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.Time;
        var elapsedTime = (float)time.ElapsedTime;
        state.CompleteDependency();

        foreach (var knot in SystemAPI.Query<DynamicBuffer<ClimbKnot>>())
        {
            var index = 1;
            while (index < knot.Length && knot[index].Time < elapsedTime)
            {
                index++;
            }

            if (index <= 1) continue;
            index--;

            knot.RemoveRange(0, index);
        }

        foreach (var knot in SystemAPI.Query<DynamicBuffer<RotateKnot>>())
        {
            var index = 1;
            while (index < knot.Length && knot[index].Time < elapsedTime)
            {
                index++;
            }

            if (index <= 1) continue;
            index--;

            knot.RemoveRange(0, index);
        }

        ref var cellHolder = ref SystemAPI.GetSingletonRW<CellHolder>().ValueRW;
        foreach (var (knot, entity) in SystemAPI.Query<DynamicBuffer<MultiPosition>>().WithEntityAccess())
        {
            var index = 1;
            while (index < knot.Length && knot[index].Time < elapsedTime)
            {
                index++;
            }

            if (index <= 1) continue;
            index--;

            for (var i = 0; i < index; i++)
            {
                cellHolder.SetPushable(knot[i].Position, Entity.Null);
                cellHolder.SetSolid(knot[i].Position, false);
            }
            knot.RemoveRange(0, index);
        }
    }
}