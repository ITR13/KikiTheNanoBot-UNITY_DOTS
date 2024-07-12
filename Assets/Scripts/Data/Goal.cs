using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct Goal : IComponentData
    {
        public bool Active;
        public double WinAtTime;
        public int3 Position;
    }
}