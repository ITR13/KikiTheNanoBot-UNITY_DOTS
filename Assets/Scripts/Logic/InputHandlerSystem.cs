using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Logic
{
    [UpdateInGroup(typeof(ControlSystemGroup), OrderFirst = true)]
    public partial class InputHandlerSystem : SystemBase
    {
        private InputAction _move, _jump, _push, _shoot, _look, _reset, _nextLevel, _previousLevel;

        protected override void OnCreate()
        {
            RequireForUpdate<InputComponent>();
        }

        protected override void OnStartRunning()
        {
            var actions = SystemAPI.GetSingleton<InputComponent>().InputActions.Value;
            _move = actions.FindAction("Move");
            _jump = actions.FindAction("Jump");
            _push = actions.FindAction("Push");
            _shoot = actions.FindAction("Shoot");
            _look = actions.FindAction("Look");

            _reset = actions.FindAction("Reset");
            _nextLevel = actions.FindAction("NextLevel");
            _previousLevel = actions.FindAction("PreviousLevel");
        }

        protected override void OnUpdate()
        {
            InputSystem.Update();

            ref var inputComponent = ref SystemAPI.GetSingletonRW<InputComponent>().ValueRW;
            inputComponent.Move = new float2(_move.ReadValue<Vector2>());
            inputComponent.Jump = ReadButton(_jump);
            inputComponent.Push = ReadButton(_push);
            inputComponent.Shoot = ReadButton(_shoot);
            inputComponent.Look = new float2(_look.ReadValue<Vector2>());

            inputComponent.Reset = ReadButton(_reset);
            inputComponent.NextLevel = ReadButton(_nextLevel);
            inputComponent.PreviousLevel = ReadButton(_previousLevel);
        }

        private InputComponent.ButtonState ReadButton(InputAction action)
        {
            return new InputComponent.ButtonState
            {
                PressedThisFrame = action.WasPressedThisFrame(),
                CurrentlyPressed = action.IsPressed(),
                ReleasedThisFrame = action.WasReleasedThisFrame(),
            };
        }
    }
}