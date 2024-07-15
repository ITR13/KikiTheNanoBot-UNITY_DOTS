using Unity.Entities;

namespace Data
{
    public struct GearRotation : IComponentData
    {
        public float Normalized;
        public float Rotations;
    }
}