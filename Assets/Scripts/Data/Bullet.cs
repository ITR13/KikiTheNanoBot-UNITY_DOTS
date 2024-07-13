using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct Bullet : IComponentData
    {
        public const float Speed = 25;
        public float3 Forward;
        public float3 Start;
        public float StartTime;
    }
}