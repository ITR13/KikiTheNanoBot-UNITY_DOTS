using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial struct FollowPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            foreach (var (followPosition, localTransform) in SystemAPI.Query<FollowPosition, RefRW<LocalTransform>>())
            {
                var otherTransform = localTransformLookup[followPosition.Entity];
                localTransform.ValueRW.Position = otherTransform.Position;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}