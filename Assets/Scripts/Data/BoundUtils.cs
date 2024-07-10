using System;
using Unity.Burst;
using Unity.Mathematics;

namespace Data
{
    public static class BoundUtils
    {
        public static int PositionToIndex(int3 bounds, int3 position)
        {
            return ((position.z * bounds.y) + position.y) * bounds.x + position.x;
        }

        public static bool IsOutOfBounds(int3 bounds, int3 position)
        {
            return math.any(position < 0 | position >= bounds);
        }
        
        [BurstDiscard]
        public static void ThrowIfOutOfBounds(int3 bounds, int3 position)
        {
            if (!IsOutOfBounds(bounds, position)) return;
            throw new ArgumentOutOfRangeException(
                nameof(position),
                $"Position {position} is not in bounds {int3.zero} -> {bounds}"
            );
        }
    }
}