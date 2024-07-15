using Unity.Entities;

namespace Data
{
    public struct EnabledIfPowered : IBufferElementData
    {
        public Entity Entity;
    }
}