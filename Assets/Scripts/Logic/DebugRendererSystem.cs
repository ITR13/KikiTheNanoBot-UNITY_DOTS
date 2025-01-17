using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Logic
{
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(RenderSystemGroup), OrderLast = true)]
    internal partial struct DebugRendererSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Room>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var offset = float3.zero;

            foreach (var transform in SystemAPI.Query<LocalToWorld>())
            {
                var startPosition = transform.Position + offset;
                Debug.DrawLine(startPosition, startPosition + transform.Right * 0.25f, Color.red);
                Debug.DrawLine(startPosition, startPosition + transform.Up * 0.25f, Color.green);
                Debug.DrawLine(startPosition, startPosition + transform.Forward * 0.25f, Color.blue);
            }

            var room = SystemAPI.GetSingleton<Room>();
            var start = new Vector3(0, 0, 0);
            var end = (Vector3)(float3)room.Bounds - new Vector3(1, 1, 1);


            for (var i = 1; i < 0b111; i++)
            {
                var pos = end * new float3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
                Debug.DrawLine(start, pos, Color.black);
                Debug.DrawLine(end, pos, Color.black);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}