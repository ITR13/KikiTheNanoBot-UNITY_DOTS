using Unity.Entities;
using Unity.Rendering;

namespace Data
{
    [MaterialProperty("_Speed")]
    public struct GearSpeed : IComponentData
    {
        public float Speed;
    }
}