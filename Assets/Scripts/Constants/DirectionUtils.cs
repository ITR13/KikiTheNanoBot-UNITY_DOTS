using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Enums
{
    public static class DirectionUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Direction IndexToDirection(int index)
        {
            return (Direction)(1 << index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int3, int) IndexToVectorAndCounterpart(int index)
        {
            var vector = int3.zero;
            var mod = index % 3;
            var div = index / 3;

            vector[mod] = div * 2 - 1;
            var counterPart = (1 - div) * 3 + mod;

            return (vector, counterPart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<int3> VectorToSurrounding(int3 vector)
        {
            var array = new NativeArray<int3>(4, Allocator.Temp);
            array[0] = new int3(vector.z, vector.x, vector.y);
            array[1] = new int3(-vector.z, -vector.x, -vector.y);
            array[2] = new int3(vector.y, vector.z, vector.x);
            array[3] = new int3(-vector.y, -vector.z, -vector.x);

            return array;
        }
    }
}