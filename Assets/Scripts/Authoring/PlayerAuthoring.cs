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
                    FallForwardDeadline = float.NegativeInfinity,
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

            AddComponent<Fall>(entity);
            SetComponentEnabled<Fall>(entity, false);
        }
    }
}