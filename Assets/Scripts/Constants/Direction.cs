using System;

namespace Enums
{
    [Flags]
    public enum Direction
    {
        None = 0,
        Left = 1 << 0,
        Down = 1 << 1,
        Back = 1 << 2,
        Right = 1 << 3,
        Up = 1 << 4,
        Forward = 1 << 5,
    }
}