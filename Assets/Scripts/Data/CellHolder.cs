using Constants;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Data
{
    public struct CellHolder : ICleanupComponentData
    {
        public int3 RoomBounds;
        public int Volume;

        public NativeBitArray SolidPositions;
        public NativeArray<Entity> PushablePositions;

        public NativeArray<Direction> Wires;
        public NativeArray<int> WiresGroup;
        public NativeBitArray PoweredGroups;
        public int WireGroupCount;

        public void Dispose()
        {
            SolidPositions.Dispose();
            PushablePositions.Dispose();
            Wires.Dispose();
            WiresGroup.Dispose();
            PoweredGroups.Dispose();
        }
    }
}