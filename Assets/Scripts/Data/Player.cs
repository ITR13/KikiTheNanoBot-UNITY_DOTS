using Unity.Entities;

namespace Data
{
    public struct Player : IComponentData
    {
        public Entity BulletPrefab;

        public Entity MoveAudio, PushAudio, JumpAudio, GoalAudio;


        public int Moves;
    }
}