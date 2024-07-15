using Unity.Collections;
using Unity.Entities;

namespace Data
{
    public struct LoadedLevel : IComponentData
    {
        public int CurrentLevelIndex;
        public Entity Entity;

        public int ExpectedMoves;
        public FixedString32Bytes Name;
    }
}