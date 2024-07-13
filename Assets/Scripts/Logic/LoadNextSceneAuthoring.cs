using Data;
using Enums;
using Unity.Burst;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace Logic
{
    public partial struct LoadNextSceneAuthoring : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelData>();
            state.RequireForUpdate<LoadedLevel>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            var loadedLevel = SystemAPI.GetSingleton<LoadedLevel>();

            if (loadedLevel.Entity != Entity.Null)
            {
                if (!SystemAPI.HasSingleton<Goal>()) return;

                // ReSharper disable once Unity.Entities.SingletonMustBeRequested
                var goal = SystemAPI.GetSingleton<Goal>();
                var time = SystemAPI.Time.ElapsedTime;
                if (time < goal.WinAtTime) return;
                SceneSystem.UnloadScene(state.WorldUnmanaged, loadedLevel.Entity);
            }

            var levels = SystemAPI.GetSingletonBuffer<LevelData>();
            var sceneEntity = SceneSystem.LoadSceneAsync(
                state.WorldUnmanaged,
                levels[loadedLevel.NextLevelIndex].SceneReference,
                new SceneSystem.LoadParameters
                {
                    AutoLoad = true,
                    Flags = SceneLoadFlags.LoadAdditive,
                    Priority = 0,
                }
            );
            SystemAPI.SetSingleton(
                new LoadedLevel { Entity = sceneEntity, NextLevelIndex = loadedLevel.NextLevelIndex + 1 }
            );
        }
    }
}