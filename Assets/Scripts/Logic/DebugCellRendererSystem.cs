using Data;
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

                    index++;
                }
            }
        }
    }
}