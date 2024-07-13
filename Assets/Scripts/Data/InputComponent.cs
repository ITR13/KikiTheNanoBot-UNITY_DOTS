using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace Data
{
    public struct InputComponent : IComponentData
    {
        public struct ButtonState
        {
            public bool CurrentlyPressed;
            public bool PressedThisFrame, ReleasedThisFrame;
        }

        public UnityObjectRef<InputActionAsset> InputActions;

        public float2 Move;
        public ButtonState Jump, Push, Shoot;
        public float2 Look;
        public ButtonState Reset, NextLevel, PreviousLevel;
    }
}