using Unity.Entities;

namespace Data
{
    public struct Player : IComponentData
    {
        public float FallForwardDeadline;
    }
}