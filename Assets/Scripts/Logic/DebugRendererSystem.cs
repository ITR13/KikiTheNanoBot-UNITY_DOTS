using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
[UpdateInGroup(typeof(RenderSystemGroup), OrderLast = true)]
partial struct DebugRendererSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Room>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var transform in SystemAPI.Query<LocalToWorld>())
        {
            Debug.DrawLine(transform.Position, transform.Position + transform.Right * 0.5f, Color.red);
            Debug.DrawLine(transform.Position, transform.Position + transform.Up * 0.5f, Color.green);
            Debug.DrawLine(transform.Position, transform.Position + transform.Forward * 0.5f, Color.blue);
        }

        var room = SystemAPI.GetSingleton<Room>();
        var start = new Vector3(-0.5f, -0.5f, -0.5f);
        var end = (Vector3)(float3)room.Dimensions + new Vector3(0.5f, 0.5f, 0.5f);

        for (var i = 1; i < 0b111; i++)
        {
            var pos = room.Dimensions * new float3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
            Debug.DrawLine(start, pos, Color.black);
            Debug.DrawLine(end, pos, Color.black);
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}