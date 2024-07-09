using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class PlayerAuthoring : MonoBehaviour
{
}

class PlayerAuthoringBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<Player>(entity);
        AddComponent<PushableTag>(entity);

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

        var rotateKnots = AddBuffer<RotateKnot>(entity);
        rotateKnots.Add(
            new RotateKnot()
            {
                Rotation = Quaternion.identity,
                Time = 0,
            }
        );

        AddComponent<Fall>(entity);
        SetComponentEnabled<Fall>(entity, false);
    }
}