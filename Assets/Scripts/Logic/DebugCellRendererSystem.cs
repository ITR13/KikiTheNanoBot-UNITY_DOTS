using Data;
using Enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
[UpdateInGroup(typeof(RenderSystemGroup), OrderLast = true)]
partial struct DebugCellRendererSystem : ISystem
{
    private NativeList<Matrix4x4> _fullCubes;
    private NativeList<Matrix4x4> _pushableCubes;
    private static readonly int WireColor = Shader.PropertyToID("_WireColor");

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DebugHolder>();
        state.RequireForUpdate<CellHolder>();

        _fullCubes = new NativeList<Matrix4x4>(64, Allocator.Domain);
        _pushableCubes = new NativeList<Matrix4x4>(64, Allocator.Domain);
    }

    public void OnDestroy(ref SystemState state)
    {
        _fullCubes.Dispose();
        _pushableCubes.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.CompleteDependency();
        var cells = SystemAPI.GetSingleton<CellHolder>();
        UpdateArrays(cells);

        var debug = SystemAPI.GetSingleton<DebugHolder>();
        var roomBoundsF = (float3)cells.RoomBounds;

        RenderParams rp = new RenderParams(debug.Material);
        rp.worldBounds = new Bounds(roomBoundsF / 2, roomBoundsF);
        rp.matProps = new MaterialPropertyBlock();
        rp.matProps.SetColor(WireColor, Color.black);
        Graphics.RenderMeshInstanced(rp, debug.Mesh, 0, _fullCubes.AsArray());

        rp.matProps.SetColor(WireColor, Color.yellow);
        Graphics.RenderMeshInstanced(rp, debug.Mesh, 0, _pushableCubes.AsArray());
    }

    [BurstCompile]
    private void UpdateArrays(CellHolder cells)
    {
        _fullCubes.Clear();
        _pushableCubes.Clear();

        var colors = new[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
        };

        var index = 0;
        for (var z = 0; z < cells.RoomBounds.z; z++)
        {
            for (var y = 0; y < cells.RoomBounds.y; y++)
            {
                for (var x = 0; x < cells.RoomBounds.x; x++)
                {
                    if (cells.SolidPositions.IsSet(index))
                    {
                        var posF = Matrix4x4.TRS(
                            new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
                            Quaternion.identity,
                            new Vector3(0.5f, 0.5f, 0.5f)
                        );
                        _fullCubes.Add(posF);
                    }

                    if (cells.PushablePositions[index] != Entity.Null)
                    {
                        var posF = Matrix4x4.TRS(
                            new Vector3(x + 0.5f, y + 0.5f, z + 0.5f),
                            Quaternion.identity,
                            new Vector3(0.25f, 0.25f, 0.25f)
                        );
                        _pushableCubes.Add(posF);
                    }

                    if (cells.WiresGroup[index] != -1)
                    {
                        var color = colors[cells.WiresGroup[index] % colors.Length];
                        DrawWires(cells, color, index, x, y, z);
                    }

                    index++;
                }
            }
        }
    }

    private static void DrawWires(CellHolder cells, Color color, int index, int x, int y, int z)
    {
        var directions = cells.Wires[index];

        for (var i = 0; i < 6; i++)
        {
            var direction = DirectionUtils.IndexToDirection(i);

            if ((directions & direction) == Direction.None) continue;

            var (vector, _) = DirectionUtils.IndexToVectorAndCounterpart(i);
            var surrounding = DirectionUtils.VectorToSurrounding(vector);

            var center = new float3(x + 0.5f, y + 0.5f, z + 0.5f) + (float3)vector * 0.5f;

            for (var j = 0; j < 2; j++)
            {
                var left = center + (float3)surrounding[j * 2] * 0.25f;
                var rigth = center + (float3)surrounding[j * 2 + 1] * 0.25f;

                Debug.DrawLine(left, rigth, color);
            }
        }
    }
}