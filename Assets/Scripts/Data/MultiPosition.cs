using Unity.Entities;
using Unity.Mathematics;

public struct MultiPosition : IBufferElementData
{
    public float Time;
    public int3 Position;
}
