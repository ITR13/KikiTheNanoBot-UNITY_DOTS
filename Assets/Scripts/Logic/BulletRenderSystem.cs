using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    public partial struct BulletRenderSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (bullet, transform) in SystemAPI.Query<Bullet, RefRW<LocalTransform>>())
            {
                transform.ValueRW.Position = bullet.Start + bullet.Forward * (time - bullet.StartTime) * Bullet.Speed;
            }
        }
    }
}