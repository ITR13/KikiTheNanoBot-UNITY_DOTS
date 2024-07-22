using Unity.Entities;

namespace Data
{
    public struct WheelKnot : IBufferElementData
    {
        public float Time;
        public float LeftRotation, RightRotation;
    }
}