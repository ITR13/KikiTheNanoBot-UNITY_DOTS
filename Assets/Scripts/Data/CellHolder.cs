using Enums;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct CellHolder : ICleanupComponentData
{
    public int3 RoomBounds;
    public NativeBitArray SolidPositions;
    public NativeArray<Entity> PushablePositions;

    public NativeArray<Direction> Wires;
    public NativeArray<int> WiresGroup;
}
