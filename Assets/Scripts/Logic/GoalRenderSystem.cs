using Constants;
using Data;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor)]
    public partial struct GoalRenderSystem : ISystem
    {
        private ComponentType _goalType;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Goal>();
            _goalType = typeof(Goal);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var goalQuery = SystemAPI.QueryBuilder().WithAll<Goal>().Build();
            goalQuery.SetChangedVersionFilter(_goalType);
            if (goalQuery.IsEmpty) return;
            var goal = goalQuery.GetSingleton<Goal>();
            var property = ShaderProperties.Instance.Data.GoalAlpha;
            Shader.SetGlobalFloat(property, goal.Active ? 1f : 0.5f);
        }
    }
}