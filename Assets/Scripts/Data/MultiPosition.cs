using Constants;
using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct MultiPosition : IBufferElementData
    {
        public float Time;
        public int3 Position;
        public ClimbFlags Flags;
    }
}