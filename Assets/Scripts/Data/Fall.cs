using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct Fall : IComponentData, IEnableableComponent
    {
        public int3 Direction;
        public float Duration;
    }
}