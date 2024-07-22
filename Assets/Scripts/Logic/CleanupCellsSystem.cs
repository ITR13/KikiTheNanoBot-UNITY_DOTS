using Data;
using Unity.Burst;
using Unity.Entities;

namespace Logic
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    internal partial struct CleanupCellsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (cellsRef, entity) in SystemAPI.Query<RefRW<CellHolder>>()
                         .WithAbsent<Room>()
                         .WithEntityAccess())
            {
                ref var cells = ref cellsRef.ValueRW;
                cells.Dispose();
                ecb.RemoveComponent<CellHolder>(entity);
            }
        }
    }
}