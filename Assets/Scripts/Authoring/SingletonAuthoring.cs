using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

class SingletonAuthoring : MonoBehaviour
{
    public int3 RoomDimensions;
    public InputActionAsset InputActionAsset;

    public Mesh DebugMesh;
    public Material DebugMaterial;
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
                Bounds = authoring.RoomDimensions,
            }
        );

        AddComponent<MovementTick>(entity);

        AddComponent(
            entity,
            new InputActionsHolder
            {
                InputActions = authoring.InputActionAsset,
            }
        );

        AddComponent(
            entity,
            new DebugHolder
            {
                Material = authoring.DebugMaterial,
                Mesh = authoring.DebugMesh,
            }
        );
    }
}