using System.Collections.Generic;
using Data;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Authoring
{
    public class LevelDataAuthoring : MonoBehaviour
    {
        public List<SceneAsset> Levels;

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
                foreach (var level in authoring.Levels)
                {
                    var reference = new EntitySceneReference(level);
                    buffer.Add(
                        new LevelData
                        {
                            SceneReference = reference,
                        }
                    );
                }
            }
        }
    }
}
#endif