using Data;
using Unity.Burst;
using Unity.Entities;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    public partial struct EnabledIfPoweredSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellHolder>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            foreach (var (buffer, powered) in SystemAPI.Query<DynamicBuffer<EnabledIfPowered>, WireCube>()
                         .WithAbsent<Goal>())
            {
                var isPowered = powered.Powered;
                foreach (var toChange in buffer)
                    if (state.EntityManager.IsEnabled(toChange.Entity) != isPowered)
                        ecb.SetEnabled(toChange.Entity, isPowered);
            }

            var cells = SystemAPI.GetSingleton<CellHolder>();
            foreach (var (buffer, wire) in SystemAPI.Query<DynamicBuffer<EnabledIfPowered>, Wire>())
            {
                var isPowered = cells.PoweredGroups.IsSet(wire.Group);
                foreach (var toChange in buffer)
                    if (state.EntityManager.IsEnabled(toChange.Entity) != isPowered)
                        ecb.SetEnabled(toChange.Entity, isPowered);
            }

            foreach (var (buffer, switchEnabled) in SystemAPI
                         .Query<DynamicBuffer<EnabledIfPowered>, EnabledRefRO<DisabledSwitchTag>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                var isPowered = !switchEnabled.ValueRO;
                foreach (var toChange in buffer)
                    if (state.EntityManager.IsEnabled(toChange.Entity) != isPowered)
                        ecb.SetEnabled(toChange.Entity, isPowered);
            }

            foreach (var (buffer, goal) in SystemAPI.Query<DynamicBuffer<EnabledIfPowered>, Goal>())
            {
                var isPowered = goal.Active;
                foreach (var toChange in buffer)
                    if (state.EntityManager.IsEnabled(toChange.Entity) != isPowered)
                        ecb.SetEnabled(toChange.Entity, isPowered);
            }
        }
    }
}