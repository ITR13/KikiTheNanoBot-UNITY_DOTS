using Unity.Entities;

namespace Data
{
    public struct OneShotAudioReference : IComponentData
    {
        public Entity Entity;
    }
}