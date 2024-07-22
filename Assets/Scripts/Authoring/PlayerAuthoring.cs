using Constants;
using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    internal class PlayerAuthoring : MonoBehaviour
    {
        public GameObject Bullet;

        public AudioOneShotAuthoring MoveAudio, PushAudio, JumpAudio, GoalAudio;
        public AudioOneShotAuthoring LandAudio;
    }

    internal class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(
                entity,
                new Player
                {
                    BulletPrefab = GetEntity(authoring.Bullet, TransformUsageFlags.Renderable),

                    JumpAudio = GetEntity(authoring.JumpAudio, TransformUsageFlags.None),
                    PushAudio = GetEntity(authoring.PushAudio, TransformUsageFlags.None),
                    MoveAudio = GetEntity(authoring.MoveAudio, TransformUsageFlags.None),
                    GoalAudio = GetEntity(authoring.GoalAudio, TransformUsageFlags.None),
                }
            );
            AddComponent<PushableTag>(entity);
            AddComponent<SolidTag>(entity);

            var climbKnots = AddBuffer<ClimbKnot>(entity);
            var transform = authoring.transform;
            var position = (float3)transform.position;
            var rotation = (quaternion)transform.rotation;
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = position,
                    Rotation = rotation,
                    Time = 0,
                    Flags = ClimbFlags.None,
                }
            );

            var multiPositions = AddBuffer<MultiPosition>(entity);
            multiPositions.Add(
                new MultiPosition
                {
                    Position = (int3)math.round(position),
                    Time = 0,
                    Flags = ClimbFlags.None,
                }
            );

            var rotateKnots = AddBuffer<RotateKnot>(entity);
            rotateKnots.Add(
                new RotateKnot
                {
                    Rotation = Quaternion.identity,
                    Time = 0,
                }
            );

            var wheelKnots = AddBuffer<WheelKnot>(entity);
            wheelKnots.Add(
                new WheelKnot
                {
                    Time = 0,
                    LeftRotation = 0,
                    RightRotation = 0,
                }
            );

            AddComponent<Fall>(entity);
            SetComponentEnabled<Fall>(entity, false);

            AddComponent(
                entity,
                new OneShotAudioReference { Entity = GetEntity(authoring.LandAudio, TransformUsageFlags.None) }
            );
        }
    }
}