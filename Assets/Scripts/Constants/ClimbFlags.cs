using System;

namespace Enums
{
    [Flags]
    public enum ClimbFlags
    {
        None = 0,
        Move = 1 << 0,
        ClimbUp = 1 << 1,
        ClimbDown = 1 << 2,

        Jump = 1 << 3,
        JumpForward = 1 << 4,

        Fall = 1 << 5,
        FallForward = 1 << 6,
    }
}