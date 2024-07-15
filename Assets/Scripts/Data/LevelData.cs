using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;

namespace Data
{
    public struct LevelData : IBufferElementData
    {
        public FixedString32Bytes LevelName;
        public int ExpectedMoves;
        public EntitySceneReference SceneReference;
    }
}