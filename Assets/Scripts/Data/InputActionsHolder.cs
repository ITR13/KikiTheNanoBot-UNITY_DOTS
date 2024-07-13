using Unity.Entities;
using UnityEngine.InputSystem;

public struct InputActionsHolder : IComponentData
{
    public UnityObjectRef<InputActionAsset> InputActions;
}