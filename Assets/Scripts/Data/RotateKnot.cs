using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct RotateKnot : IBufferElementData
    {
        public float Time;
        public quaternion Rotation;
    }
}