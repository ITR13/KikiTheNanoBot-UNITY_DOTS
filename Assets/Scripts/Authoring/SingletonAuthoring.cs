using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class SingletonAuthoring : MonoBehaviour
{
    public int3 RoomDimensions;
}

class SingletonAuthoringBaker : Baker<SingletonAuthoring>
{
    public override void Bake(SingletonAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(
            entity,
            new Room
            {
                Dimensions = authoring.RoomDimensions,
            }
        );

        AddComponent<MovementTick>(entity);
    }
}