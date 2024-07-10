using Unity.Entities;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(InitializeCellsSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    public partial struct RegenerateCellsOnMovedObject : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellHolder>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var tQuery = SystemAPI.QueryBuilder().WithAll<LocalToWorld>().Build();
            tQuery.SetChangedVersionFilter(new ComponentType(typeof(LocalToWorld)));
            tQuery.AddOrderVersionFilter();

            if (tQuery.IsEmpty) return;

            var cellHolderEntity = SystemAPI.GetSingletonEntity<CellHolder>();
            var cellHolder = SystemAPI.GetComponent<CellHolder>(cellHolderEntity);

            cellHolder.Dispose();
            state.EntityManager.RemoveComponent<CellHolder>(cellHolderEntity);
        }
    }
}