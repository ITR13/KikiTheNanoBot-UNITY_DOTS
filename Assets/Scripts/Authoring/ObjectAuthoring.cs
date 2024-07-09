using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ObjectAuthoring : MonoBehaviour
{
    public bool Pushable;

    public class ObjectAuthoringBaker : Baker<ObjectAuthoring>
    {
        public override void Bake(ObjectAuthoring authoring)
        {
            var transformUsageFlags = authoring.Pushable ? TransformUsageFlags.Dynamic : TransformUsageFlags.Renderable;

            var entity = GetEntity(transformUsageFlags);

            AddComponent<SolidTag>(entity);
            if (!authoring.Pushable) return;

            AddComponent(entity, new PushableTag());

            var climbKnots = AddBuffer<ClimbKnot>(entity);
            var position = (float3)authoring.transform.position;
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = position,
                    Rotation = Quaternion.identity,
                    Time = 0,
                }
            );

            var multiPositions = AddBuffer<MultiPosition>(entity);
            multiPositions.Add(
                new MultiPosition
                {
                    Position = (int3)math.round(position),
                    Time = 0,
                }
            );

            AddComponent<Fall>(entity);
            SetComponentEnabled<Fall>(entity, false);
        }
    }
}