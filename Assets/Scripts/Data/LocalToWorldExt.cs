using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

namespace Data
{
    public static class LocalToWorldExt
    {
        public static NativeArray<int3> ToPositionArray(
            this NativeArray<LocalToWorld>.ReadOnly localToWorldArray,
            AllocatorManager.AllocatorHandle allocator
        )
        {
            var positions = CollectionHelper.CreateNativeArray<int3>(
                localToWorldArray.Length,
                allocator,
                NativeArrayOptions.UninitializedMemory
            );
            for (var i = 0; i < positions.Length; i++) positions[i] = (int3)math.round(localToWorldArray[i].Position);

            return positions;
        }
    }
}