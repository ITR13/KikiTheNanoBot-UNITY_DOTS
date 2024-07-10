using System;
using Enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Data.BoundUtils;

namespace Data
{
    public static class CellHolderExt
    {
        public static bool IsSolid(this CellHolder holder, in int3 position)
        {
            if (IsOutOfBounds(holder.RoomBounds, position)) return true;
            var index = PositionToIndex(holder.RoomBounds, position);
            return holder.SolidPositions.IsSet(index);
        }

        public static bool IsPushable(this CellHolder holder, in int3 position, out Entity entity)
        {
            entity = Entity.Null;
            if (IsOutOfBounds(holder.RoomBounds, position)) return false;

            var index = PositionToIndex(holder.RoomBounds, position);
            entity = holder.PushablePositions[index];
            return entity != Entity.Null;
        }

        public static void SetSolid(this CellHolder holder, int3 position, bool solid)
        {
            ThrowIfOutOfBounds(holder.RoomBounds, position);
            var index = PositionToIndex(holder.RoomBounds, position);
            ThrowIfSetTo(holder.SolidPositions, index, solid);
            holder.SolidPositions.Set(index, solid);
        }

        public static void SetPushable(this CellHolder holder, int3 position, Entity entity)
        {
            ThrowIfOutOfBounds(holder.RoomBounds, position);
            var index = PositionToIndex(holder.RoomBounds, position);
            ThrowIfWrongFill(holder.PushablePositions, index, entity != Entity.Null);
            holder.PushablePositions[index] = entity;
        }

        public static void GetWire(this CellHolder holder, int3 position, out Direction direction, out int group)
        {
            ThrowIfOutOfBounds(holder.RoomBounds, position);

            var index = PositionToIndex(holder.RoomBounds, position);
            direction = holder.Wires[index];
            group = holder.WiresGroup[index];
        }

        [BurstDiscard]
        private static void ThrowIfSetTo(NativeBitArray bitArray, int index, bool set)
        {
            if (bitArray.IsSet(index) != set) return;
            throw new ArgumentException(
                nameof(index),
                $"Tried to set index {index} to {set}, but it was already {set}"
            );
        }

        [BurstDiscard]
        private static void ThrowIfWrongFill(NativeArray<Entity> bitArray, int index, bool shouldBeEmpty)
        {
            if (bitArray[index] == Entity.Null == shouldBeEmpty) return;

            throw new ArgumentException(
                nameof(index),
                $"Index {index} already contains {bitArray[index]}"
            );
        }
    }
}