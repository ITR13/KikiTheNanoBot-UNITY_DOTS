using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct Knot : IBufferElementData
    {
        public float Time;
        public float3 Position;
        public quaternion Rotation;
    }
}