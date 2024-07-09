using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct Room : IComponentData
    {
        public int3 Bounds;
    }
}