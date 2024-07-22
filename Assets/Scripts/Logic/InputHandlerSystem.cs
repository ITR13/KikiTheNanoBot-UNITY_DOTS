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

            _move = FindAction("Move");
            _jump = FindAction("Jump");
            _push = FindAction("Push");
            _shoot = FindAction("Shoot");
            _look = FindAction("Look");

            _reset = FindAction("Reset");
            _nextLevel = FindAction("NextLevel");
            _previousLevel = FindAction("PreviousLevel");

            _nextLook = FindAction("NextLook");
            _previousLook = FindAction("PreviousLook");

            actionAsset.FindActionMap("Player").Enable();
            return;

            InputAction FindAction(string actionName)
            {
                var action = actionAsset.FindAction(actionName);
                if (action == null)
                {
                    Debug.LogError($"Failed to find action {actionName}");
                }

                return action;
            }
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
            if (action == null)
            {
                return baseState;
            }

            baseState.PressedThisFrame |= action.WasPressedThisFrame();
            baseState.CurrentlyPressed |= action.IsPressed();
            baseState.ReleasedThisFrame |= action.WasReleasedThisFrame();
            return baseState;
        }
    }
}