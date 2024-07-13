using Unity.Entities;
using UnityEngine.InputSystem;

namespace Data
{
    public struct InputActionsHolder : IComponentData
    {
        public UnityObjectRef<InputActionAsset> InputActions;
    }
}