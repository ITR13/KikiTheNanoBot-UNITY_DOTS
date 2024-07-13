using System.Linq;
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
            var actionAsset = SystemAPI.GetSingleton<InputComponent>().InputActions.Value;
            _move = actionAsset.FindAction("Move");
            _jump = actionAsset.FindAction("Jump");
            _push = actionAsset.FindAction("Push");
            _shoot = actionAsset.FindAction("Shoot");
            _look = actionAsset.FindAction("Look");

            _reset = actionAsset.FindAction("Reset");
            _nextLevel = actionAsset.FindAction("NextLevel");
            _previousLevel = actionAsset.FindAction("PreviousLevel");

            actionAsset.FindActionMap("Player").Enable();
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
            var s = $"PUSH: Enabled: {_push.enabled}   {_push.actionMap.enabled}";
            foreach (var binding in _push.bindings)
            {
                s += $"\n{binding.path}   {binding.name}\n{binding.action}";
            }

            Debug.Log(s);

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