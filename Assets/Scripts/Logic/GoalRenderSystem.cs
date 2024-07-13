using Data;
using Constants;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Logic
{
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
            Shader.SetGlobalFloat(property, goal.Active ? 0.5f : 1f);
        }
    }
}