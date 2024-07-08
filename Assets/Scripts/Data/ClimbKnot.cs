using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct ClimbKnot : IBufferElementData
    {
        public float Time;
        public float3 Position;
        public quaternion Rotation;
    }
}