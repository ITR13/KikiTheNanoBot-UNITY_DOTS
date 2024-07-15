using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;

namespace Logic
{
    public partial struct LevelControllerSystem : ISystem
    {
        private double _forceDelay;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputComponent>();
            state.RequireForUpdate<LevelData>();
            state.RequireForUpdate<LoadedLevel>();
            _forceDelay = double.NegativeInfinity;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            var loadedLevel = SystemAPI.GetSingleton<LoadedLevel>();

            var time = SystemAPI.Time.ElapsedTime;
            ref var input = ref SystemAPI.GetSingletonRW<InputComponent>().ValueRW;

            if (input.Reset.PressedThisFrame)
            {
                LoadLevel(ref state, loadedLevel, loadedLevel.CurrentLevelIndex);
            }
            else if (input.NextLevel.PressedThisFrame)
            {
                LoadLevel(ref state, loadedLevel, loadedLevel.CurrentLevelIndex + 1);
            }
            else if (input.PreviousLevel.PressedThisFrame)
            {
                LoadLevel(ref state, loadedLevel, loadedLevel.CurrentLevelIndex - 1);
            }
            else if (loadedLevel.Entity != Entity.Null)
            {
                if (!SystemAPI.HasSingleton<Goal>()) return;

                // ReSharper disable once Unity.Entities.SingletonMustBeRequested
                var goal = SystemAPI.GetSingleton<Goal>();
                if (time < goal.WinAtTime) return;
                LoadLevel(ref state, loadedLevel, loadedLevel.CurrentLevelIndex + 1);
            }
            else
            {
                if (time < _forceDelay) return;
                _forceDelay = time + 1;
                LoadLevel(ref state, loadedLevel, loadedLevel.CurrentLevelIndex + 1);
            }


            input.Reset = default;
            input.NextLevel = default;
            input.PreviousLevel = default;
        }

        private void LoadLevel(ref SystemState state, LoadedLevel loadedLevel, int level)
        {
            Debug.Log($"Loading level {level}");
            if (loadedLevel.Entity != Entity.Null)
            {
                SceneSystem.UnloadScene(
                    state.WorldUnmanaged,
                    loadedLevel.Entity,
                    SceneSystem.UnloadParameters.DestroyMetaEntities
                );
                SystemAPI.SetSingleton(
                    new LoadedLevel { Entity = Entity.Null, CurrentLevelIndex = loadedLevel.CurrentLevelIndex }
                );
            }

            var levels = SystemAPI.GetSingletonBuffer<LevelData>();
            level = math.clamp(level, 0, levels.Length - 1);

            var levelData = levels[level];
            var nextScene = levelData.SceneReference;

            var sceneEntity = SceneSystem.LoadSceneAsync(
                state.WorldUnmanaged,
                nextScene,
                new SceneSystem.LoadParameters
                {
                    AutoLoad = true,
                    Flags = SceneLoadFlags.LoadAdditive,
                    Priority = 0,
                }
            );
            SystemAPI.SetSingleton(
                new LoadedLevel
                {
                    Entity = sceneEntity, CurrentLevelIndex = level, ExpectedMoves = levelData.ExpectedMoves,
                    Name = levelData.LevelName
                }
            );
        }
    }
}