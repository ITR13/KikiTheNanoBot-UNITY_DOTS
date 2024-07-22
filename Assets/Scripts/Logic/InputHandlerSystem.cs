using System.IO;
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
        private InputAction _nextLook, _previousLook;

        protected override void OnCreate()
        {
            RequireForUpdate<InputComponent>();
        }

        protected override void OnStartRunning()
        {
            var actionAsset = SystemAPI.GetSingleton<InputComponent>().InputActions.Value;
            var bindingsPath = Path.Combine(Application.persistentDataPath, "Controls.json");
            if (File.Exists(bindingsPath))
                actionAsset.LoadFromJson(File.ReadAllText(bindingsPath));
            else
                File.WriteAllText(bindingsPath, actionAsset.ToJson());

            _move = actionAsset.FindAction("Move");
            _jump = actionAsset.FindAction("Jump");
            _push = actionAsset.FindAction("Push");
            _shoot = actionAsset.FindAction("Shoot");
            _look = actionAsset.FindAction("Look");

            _reset = actionAsset.FindAction("Reset");
            _nextLevel = actionAsset.FindAction("NextLevel");
            _previousLevel = actionAsset.FindAction("PreviousLevel");

            _nextLook = actionAsset.FindAction("NextLook");
            _previousLook = actionAsset.FindAction("PreviousLook");

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

            // We these needs to save their states across multiple frames due to fixed update happening more frequently than update
            inputComponent.Reset = ReadButton(_reset, inputComponent.Reset);
            inputComponent.NextLevel = ReadButton(_nextLevel, inputComponent.NextLevel);
            inputComponent.PreviousLevel = ReadButton(_previousLevel, inputComponent.PreviousLevel);

            inputComponent.NextLook = ReadButton(_nextLook, inputComponent.NextLook);
            inputComponent.PreviousLook = ReadButton(_previousLook, inputComponent.PreviousLook);
        }

        private InputComponent.ButtonState ReadButton(
            InputAction action,
            InputComponent.ButtonState baseState = default
        )
        {
            baseState.PressedThisFrame |= action.WasPressedThisFrame();
            baseState.CurrentlyPressed |= action.IsPressed();
            baseState.ReleasedThisFrame |= action.WasReleasedThisFrame();
            return baseState;
        }
    }
}