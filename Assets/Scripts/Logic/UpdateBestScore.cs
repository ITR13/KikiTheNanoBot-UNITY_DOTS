using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial struct UpdateBestScore : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LoadedLevel>();
            state.RequireForUpdate<Goal>();
            state.RequireForUpdate<Player>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var playerComponent = SystemAPI.GetComponent<Player>(playerEntity);
            var goal = SystemAPI.GetSingleton<Goal>();
            var multiPositions = SystemAPI.GetBuffer<MultiPosition>(playerEntity);

            var lastPosition = multiPositions[^1].Position;
            if (!goal.Active || !math.all(lastPosition == goal.Position)) return;

            var levelData = SystemAPI.GetSingleton<LoadedLevel>();
            var key = "Best-" + levelData.Name;
            var bestMoves = PlayerPrefs.GetInt(key, 10000);

            if (playerComponent.Moves >= bestMoves) return;

            PlayerPrefs.SetInt(key, playerComponent.Moves);
        }
    }
}