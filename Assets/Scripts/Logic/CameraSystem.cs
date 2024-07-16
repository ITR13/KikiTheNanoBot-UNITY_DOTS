using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(RenderSystemGroup))]
    [UpdateAfter(typeof(KnotRenderPosSystem))]
    public partial struct CameraSystem : ISystem
    {
        private static readonly float3[] LookOffsets = new[]
        {
            new float3(0, 1, -3),
            new float3(0, 0, 0),
            new float3(0, 2, -6),
            new float3(2, 2, -3),
            new float3(-2, 2, -3),
        };

        private int _currentLook;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputComponent>();
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<CameraBridge>();
        }

        public void OnUpdate(ref SystemState state)
        {
            ref var cameraBridge = ref SystemAPI.GetSingletonRW<CameraBridge>().ValueRW;
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();

            var deltaTime = SystemAPI.Time.DeltaTime;

            ref var input = ref SystemAPI.GetSingletonRW<InputComponent>().ValueRW;
            if (input.NextLook.PressedThisFrame)
            {
                _currentLook = (_currentLook + 1) % LookOffsets.Length;
            }

            if (input.PreviousLook.PressedThisFrame)
            {
                _currentLook = (_currentLook + LookOffsets.Length - 1) % LookOffsets.Length;
            }

            input.NextLook.PressedThisFrame = default;
            input.PreviousLevel.PressedThisFrame = default;


            var look = MoveTowards(cameraBridge.CurrentLookRotation, input.Look, deltaTime * 4);
            cameraBridge.CurrentLookRotation = look;

            look = (math.smoothstep(new float2(-1, -1), new float2(1, 1), look) - 0.5f) * math.PI;

            var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
            playerTransform = playerTransform.Rotate(quaternion.EulerXYZ(look.y, look.x, 0));

            var position = playerTransform.TransformPoint(LookOffsets[_currentLook]);
            var rotation = playerTransform.Rotation;


            if (!cameraBridge.Spawned)
            {
                cameraBridge.Spawned = true;
                cameraBridge.Camera = Object.Instantiate<GameObject>(cameraBridge.Camera);
            }

            var cameraTransform = cameraBridge.Camera.Value.transform;
            cameraTransform.position = position;
            cameraTransform.rotation = rotation;
        }

        private float2 MoveTowards(float2 from, float2 to, float maxDelta)
        {
            var distance = math.distance(from, to);

            if (distance < maxDelta)
            {
                return to;
            }

            return from + (to - from) * maxDelta / distance;
        }
    }
}