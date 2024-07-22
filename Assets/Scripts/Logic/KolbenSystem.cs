using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    public partial struct KolbenSystem : ISystem
    {
        private const float Period = 0.5f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var normalizedTime = math.frac(time / Period);
            if (normalizedTime <= 0.9f)
                normalizedTime /= 0.9f;
            else
                normalizedTime = (1 - normalizedTime) * 10;

            foreach (var (kolben, localTransform) in SystemAPI.Query<Kolben, RefRW<LocalTransform>>())
                localTransform.ValueRW.Position = kolben.StartPosition + new float3(0, normalizedTime * 0.2f, 0);
        }
    }
}