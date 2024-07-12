using Unity.Entities;

namespace Data
{
    public struct LoadedLevel : IComponentData
    {
        public int NextLevelIndex;
        public Entity Entity;
    }
}