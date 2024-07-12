using System.Collections.Generic;
using Data;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

#if UNITY_EDITOR
public class LevelDataAuthoring : MonoBehaviour
{
    public List<UnityEditor.SceneAsset> Levels;

    class Baker : Baker<LevelDataAuthoring>
    {
        public override void Bake(LevelDataAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<LoadedLevel>(entity);

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
#endif