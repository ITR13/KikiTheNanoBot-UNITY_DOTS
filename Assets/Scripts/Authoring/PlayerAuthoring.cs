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

        var climbKnots = AddBuffer<ClimbKnot>(entity);
        climbKnots.Add(
            new ClimbKnot
            {
                Position = new float3(0, 0, 0),
                Rotation = Quaternion.identity,
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
    }
}