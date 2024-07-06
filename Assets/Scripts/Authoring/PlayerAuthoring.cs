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

        var knots = AddBuffer<Knot>(entity);

        var t = 0f;
        for(var i = 0; i<10; i++)
        {
            knots.Add(
                new Knot
                {
                    Position = new float3(0, 0, i),
                    Rotation = Quaternion.identity,
                    Time = t,
                }
            );
            t += i;
        }
    }
}