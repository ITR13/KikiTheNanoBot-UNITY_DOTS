using Unity.Entities;

namespace Data
{
    public struct Player : IComponentData
    {
        public float FallForwardDeadline;
        public Entity BulletPrefab;

        public Entity MoveAudio, PushAudio, JumpAudio;
    }
}