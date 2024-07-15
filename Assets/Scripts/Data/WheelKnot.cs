using Constants;
using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct WheelKnot : IBufferElementData
    {
        public float Time;
        public float LeftRotation, RightRotation;
    }
}