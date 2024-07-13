using Unity.Entities;

namespace Data
{
    public struct LoadedLevel : IComponentData
    {
        public int CurrentLevelIndex;
        public Entity Entity;
    }
}