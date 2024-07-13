using Data;
using Enums;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    public partial struct ActivateGoalSystem : ISystem
    {
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
        }
    }
}