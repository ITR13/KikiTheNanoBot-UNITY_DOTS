using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    public partial struct UpdateUi : ISystem
    {
        private FixedString32Bytes _loadedName;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<VariableHolder>();
            state.RequireForUpdate<LoadedLevel>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var variables = SystemAPI.GetSingleton<VariableHolder>();
            var levelData = SystemAPI.GetSingleton<LoadedLevel>();
            var player = SystemAPI.GetSingleton<Player>();

            if (levelData.Name != _loadedName)
            {
                _loadedName = levelData.Name;
                if (_loadedName.Length == 0) return;

                var levelNameVariable = (StringVariable)variables.VariableGroup.Value["LevelName"];
                var highScoreVariable = (IntVariable)variables.VariableGroup.Value["BestMoves"];

                levelNameVariable.Value = _loadedName.ToString();
                highScoreVariable.Value = levelData.ExpectedMoves -
                                          PlayerPrefs.GetInt("Best-" + levelNameVariable.Value, 1000000);
            }

            var movesVariable = (IntVariable)variables.VariableGroup.Value["Moves"];
            var score = levelData.ExpectedMoves - player.Moves;
            movesVariable.Value = score;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnStartRunning(ref SystemState state)
        {
        }

        public void OnStopRunning(ref SystemState state)
        {
        }
    }
}