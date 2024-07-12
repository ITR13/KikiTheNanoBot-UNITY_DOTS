using Data;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateBefore(typeof(PlayerControllerSystem))]
    public partial struct ActivateGoalSystem : ISystem
    {
        private static readonly int GoalAlpha = Shader.PropertyToID("_GoalAlpha");

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Goal>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var goalEntity = SystemAPI.GetSingletonEntity<Goal>();
            var goal = SystemAPI.GetComponent<Goal>(goalEntity);

            var isPowered = !SystemAPI.HasComponent<WireCube>(goalEntity) ||
                            SystemAPI.GetComponent<WireCube>(goalEntity).Powered;

            if (goal.Active == isPowered) return;
            goal.Active = isPowered;
            SystemAPI.SetSingleton(goal);
            Shader.SetGlobalFloat(GoalAlpha, goal.Active ? 1f : 0.5f);
        }
    }
}