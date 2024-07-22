#if UNITY_EDITOR
using System.Collections.Generic;
using Data;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

namespace Authoring
{
    [ExecuteAlways]
    public class LevelDataAuthoring : MonoBehaviour
    {
        public List<SceneAsset> Levels;
        public List<string> LevelNames;
        public List<int> LevelMoves;

        public void Update()
        {
            while (LevelNames.Count < Levels.Count) LevelNames.Add("Lorem Ipsum");

            while (LevelMoves.Count < Levels.Count) LevelMoves.Add(0);
        }

        private class Baker : Baker<LevelDataAuthoring>
        {
            public override void Bake(LevelDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(
                    entity,
                    new LoadedLevel
                    {
                        Entity = Entity.Null,
                        CurrentLevelIndex = -1,
                    }
                );

                var buffer = AddBuffer<LevelData>(entity);
                for (var index = 0; index < authoring.Levels.Count; index++)
                {
                    var level = authoring.Levels[index];
                    var name = authoring.LevelNames[index];
                    var moves = authoring.LevelMoves[index];

                    var reference = new EntitySceneReference(level);
                    buffer.Add(
                        new LevelData
                        {
                            SceneReference = reference,
                            ExpectedMoves = moves,
                            LevelName = name,
                        }
                    );
                }
            }
        }
    }
}
#endif