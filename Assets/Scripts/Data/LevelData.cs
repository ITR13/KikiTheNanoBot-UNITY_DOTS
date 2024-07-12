using Unity.Entities;
using Unity.Entities.Serialization;

namespace Data
{
    public struct LevelData : IBufferElementData
    {
        public EntitySceneReference SceneReference;
    }
}