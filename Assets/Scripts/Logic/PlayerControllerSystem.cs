using Data;
using Enums;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[BurstCompile]
[UpdateInGroup(typeof(ControlSystemGroup))]
[UpdateBefore(typeof(FallSystem))]
internal partial class PlayerControllerSystem : SystemBase
{
    // I'm lazy, sue me
    private bool _hasJumped;
    private InputAction _move, _look, _shoot, _jump, _push;

    protected override void OnCreate()
    {
        RequireForUpdate<Player>();
        RequireForUpdate<InputActionsHolder>();
        RequireForUpdate<CellHolder>();
        RequireForUpdate<Goal>();
    }

    protected override void OnStartRunning()
    {
        var actions = SystemAPI.GetSingleton<InputActionsHolder>().InputActions.Value;
        _move = actions.FindAction("Move");
        _look = actions.FindAction("Look");
        _shoot = actions.FindAction("Shoot");
        _jump = actions.FindAction("Jump");
        _push = actions.FindAction("Push");
    }

    protected override void OnUpdate()
    {
        var direction = new float2(_move.ReadValue<Vector2>());
        var push = _push.IsPressed();
        var jumpHeld = _jump.IsPressed();
        var jumpReleased = _jump.WasReleasedThisFrame() && !_hasJumped;

        UpdateBursted(direction, push, jumpHeld, jumpReleased);
        _hasJumped &= jumpHeld;
    }

    [BurstCompile]
    private void UpdateBursted(in float2 direction, bool push, bool jumpHeld, bool jumpReleased)
    {
        CompleteDependency();
        var player = SystemAPI.GetSingletonEntity<Player>();
        var goal = SystemAPI.GetSingleton<Goal>();
        var multiPositions = SystemAPI.GetBuffer<MultiPosition>(player);

        var time = (float)SystemAPI.Time.ElapsedTime;

        if (goal.Active && math.all(multiPositions[^1].Position == goal.Position))
        {
            SystemAPI.SetComponentEnabled<Fall>(player, false);
            if (goal.WinAtTime > time + 1)
            {
                goal.WinAtTime = time + 1;
                SystemAPI.SetSingleton(goal);
            }

            return;
        }


        var climbKnots = SystemAPI.GetBuffer<ClimbKnot>(player);
        var rotateKnots = SystemAPI.GetBuffer<RotateKnot>(player);

        Climb(
            time,
            player,
            multiPositions,
            climbKnots,
            rotateKnots,
            math.sign(direction.y),
            push,
            jumpHeld,
            jumpReleased
        );

        if (direction.x != 0) Rotate(rotateKnots, math.sign(direction.x), time);
    }

    private void Climb(
        float time,
        Entity playerEntity,
        DynamicBuffer<MultiPosition> multiPositions,
        DynamicBuffer<ClimbKnot> climbKnots,
        DynamicBuffer<RotateKnot> rotateKnots,
        float multiplier,
        bool push,
        bool jumpHeld,
        bool jumpReleased
    )
    {
        var player = SystemAPI.GetComponent<Player>(playerEntity);
        var lastKnot = climbKnots[^1];
        var lastRotate = rotateKnots[^1].Rotation;

        var rotation = math.mul(lastKnot.Rotation, lastRotate.value);
        var moveDirection = math.round(math.forward(rotation)) * multiplier;
        var nextPosition = math.round(lastKnot.Position + moveDirection);

        var nextPositionI = (int3)nextPosition;
        var nextIsOwn = CheckPosition(nextPositionI, multiPositions);
        ref var cells = ref SystemAPI.GetSingletonRW<CellHolder>().ValueRW;
        var nextIsSolid = !nextIsOwn && cells.IsSolid(nextPositionI);

        var fallingForward = false;
        if (
            (lastKnot.Flags & (ClimbFlags.JumpForward | ClimbFlags.Fall)) != ClimbFlags.None &&
            time < player.FallForwardDeadline &&
            multiplier > 0 &&
            !nextIsSolid
        )
        {
            while (climbKnots.Length > 1 && climbKnots[^1].Flags == ClimbFlags.Fall) climbKnots.Length--;

            while (multiPositions.Length > 1 && multiPositions[^1].Flags == ClimbFlags.Fall)
            {
                var removedPosition = multiPositions[^1].Position;
                cells.SetSolid(removedPosition, false);
                cells.SetPushable(removedPosition, Entity.Null);
                multiPositions.Length--;
            }

            lastKnot = climbKnots[^1];
            fallingForward = true;
        }

        if (lastKnot.Time - StructConstants.CoyoteTime > time) return;

        if (lastKnot.Time + StructConstants.CoyoteTime > time)
        {
            time = lastKnot.Time;
        }
        else
        {
            lastKnot.Time = time;
            climbKnots[^1] = lastKnot;
        }


        var down = math.round(math.mul(rotation, new float3(0, -1, 0)));
        var downI = (int3)down;

        var belowUs = math.round(lastKnot.Position + down);
        var belowUsI = (int3)belowUs;
        var isGrounded = cells.IsSolid(belowUsI) && !CheckPosition(belowUsI, multiPositions);
        if (!isGrounded && !fallingForward)
        {
            SystemAPI.SetComponent(playerEntity, new Fall { Direction = downI, Duration = 0.12f });
            SystemAPI.SetComponentEnabled<Fall>(playerEntity, true);
        }

        var jumpPositionI = belowUsI - downI * 2;
        var aboveIsOwn = CheckPosition(jumpPositionI, multiPositions);
        var canJump = (aboveIsOwn || !cells.IsSolid(jumpPositionI)) && isGrounded;

        if (multiplier == 0)
        {
            if (!jumpReleased || !canJump) return;

            HandlePlayerCells(playerEntity, multiPositions, aboveIsOwn, jumpPositionI, ref cells);
            QueueJump(time, multiPositions, climbKnots, jumpPositionI, lastKnot, ClimbFlags.Jump);
            return;
        }

        var attemptJumpForward = jumpHeld && canJump && multiplier > 0;
        if (attemptJumpForward)
        {
            var jumpForwardPositionI = nextPositionI - downI;
            var jumpUpInstead = cells.IsSolid(jumpForwardPositionI) || nextIsSolid;
            var destination = jumpUpInstead ? jumpPositionI : jumpForwardPositionI;
            var isOwn = jumpUpInstead ? aboveIsOwn : CheckPosition(jumpForwardPositionI, multiPositions);
            var flags = jumpUpInstead ? ClimbFlags.Jump : ClimbFlags.JumpForward;
            if (!jumpUpInstead)
            {
                player.FallForwardDeadline = time + 0.12f + StructConstants.CoyoteTime;
                SystemAPI.SetComponent(playerEntity, player);
            }

            HandlePlayerCells(playerEntity, multiPositions, isOwn, destination, ref cells);
            QueueJump(time, multiPositions, climbKnots, destination, lastKnot, flags);
            _hasJumped = true;

            return;
        }

        var nextNextPosition = nextPositionI + (int3)moveDirection;

        if (isGrounded && !nextIsOwn && push && cells.IsPushable(nextPositionI, out var pushableEntity))
            push = HandlePushing(
                ref time,
                climbKnots,
                pushableEntity,
                nextPositionI,
                cells,
                nextNextPosition,
                downI,
                ref lastKnot
            );
        else
            push = false;

        // TODO: If slippery and not grounded, disallow climbing
        if (!push && nextIsSolid)
        {
            QueueClimbUp(time, climbKnots, multiplier, rotation, lastKnot);

            // Special case where we climb up something as we jump
            SystemAPI.SetComponentEnabled<Fall>(playerEntity, false);
            return;
        }


        var posUnderNextI = nextPositionI + downI;
        var posUnderNextIsOwn = CheckPosition(posUnderNextI, multiPositions);

        if (!posUnderNextIsOwn && cells.IsSolid(posUnderNextI))
        {
            HandlePlayerCells(playerEntity, multiPositions, nextIsOwn, nextPositionI, ref cells);
            QueueMove(time, multiPositions, climbKnots, nextPositionI, nextPosition, lastKnot);
            SystemAPI.SetComponentEnabled<Fall>(playerEntity, false);
            return;
        }

        if (fallingForward)
        {
            HandlePlayerCells(playerEntity, multiPositions, posUnderNextIsOwn, posUnderNextI, ref cells);
            QueueJump(time, multiPositions, climbKnots, posUnderNextI, lastKnot, ClimbFlags.FallForward);
            player.FallForwardDeadline = float.NegativeInfinity;
            SystemAPI.SetComponent(playerEntity, player);
            return;
        }

        if (isGrounded)
        {
            HandlePlayerCells(playerEntity, multiPositions, nextIsOwn, nextPositionI, ref cells);
            HandlePlayerCells(playerEntity, multiPositions, posUnderNextIsOwn, posUnderNextI, ref cells);
            QueueClimbDown(
                time,
                multiPositions,
                climbKnots,
                multiplier,
                nextPositionI,
                posUnderNextI,
                rotation,
                lastKnot,
                moveDirection,
                nextPosition,
                down
            );
            SystemAPI.SetComponentEnabled<Fall>(playerEntity, false);
        }
    }

    private static void QueueClimbUp(
        float time,
        DynamicBuffer<ClimbKnot> climbKnots,
        float multiplier,
        quaternion rotation,
        ClimbKnot lastKnot
    )
    {
        var left = math.round(math.mul(rotation, new float3(-1, 0, 0)));
        var climbUpRotation = quaternion.AxisAngle(left, math.PIHALF * multiplier);
        climbKnots.Add(
            new ClimbKnot
            {
                Position = lastKnot.Position,
                Rotation = math.mul(climbUpRotation, lastKnot.Rotation),
                Time = time + 0.2f,
                Flags = ClimbFlags.ClimbUp,
            }
        );
    }

    private static void QueueClimbDown(
        float time,
        DynamicBuffer<MultiPosition> multiPositions,
        DynamicBuffer<ClimbKnot> climbKnots,
        float multiplier,
        int3 nextPositionI,
        int3 posUnderNextI,
        quaternion rotation,
        ClimbKnot lastKnot,
        float3 moveDirection,
        float3 nextPosition,
        float3 down
    )
    {
        var endTime = time + 0.5f;
        var lastMultiPosition = multiPositions[^1];
        lastMultiPosition.Time = endTime;
        multiPositions[^1] = lastMultiPosition;
        multiPositions.Add(
            new MultiPosition
            {
                Position = nextPositionI,
                Time = endTime,
                Flags = ClimbFlags.ClimbDown,
            }
        );
        multiPositions.Add(
            new MultiPosition
            {
                Position = posUnderNextI,
                Time = endTime,
                Flags = ClimbFlags.ClimbDown,
            }
        );


        var right = math.round(math.mul(rotation, new float3(1, 0, 0)));
        var climbDownRotation = quaternion.AxisAngle(right, math.PIHALF * multiplier);
        var endRotation = math.mul(climbDownRotation, lastKnot.Rotation);

        climbKnots.Add(
            new ClimbKnot
            {
                Position = lastKnot.Position + moveDirection * 0.5f,
                Rotation = lastKnot.Rotation,
                Time = time + 0.2f * 0.5f,
                Flags = ClimbFlags.ClimbDown,
            }
        );
        climbKnots.Add(
            new ClimbKnot
            {
                Position = nextPosition + down * 0.5f,
                Rotation = endRotation,
                Time = time + 0.8f * 0.5f,
                Flags = ClimbFlags.ClimbDown,
            }
        );
        climbKnots.Add(
            new ClimbKnot
            {
                Position = posUnderNextI,
                Rotation = endRotation,
                Time = endTime,
                Flags = ClimbFlags.ClimbDown,
            }
        );
    }

    private static void HandlePlayerCells(
        Entity playerEntity,
        DynamicBuffer<MultiPosition> multiPositions,
        bool removeFromOwn,
        int3 position,
        ref CellHolder cells
    )
    {
        if (removeFromOwn)
        {
            for (var i = multiPositions.Length - 1; i >= 0; i--)
                if (math.all(multiPositions[i].Position == position))
                    multiPositions.RemoveAt(i);
        }
        else
        {
            cells.SetSolid(position, true);
            cells.SetPushable(position, playerEntity);
        }
    }

    private static void QueueMove(
        float time,
        DynamicBuffer<MultiPosition> multiPositions,
        DynamicBuffer<ClimbKnot> climbKnots,
        int3 nextPositionI,
        float3 nextPosition,
        ClimbKnot lastKnot
    )
    {
        var endTime = time + 0.2f;
        var lastMultiPosition = multiPositions[^1];
        lastMultiPosition.Time = endTime;
        multiPositions[^1] = lastMultiPosition;
        multiPositions.Add(
            new MultiPosition
            {
                Position = nextPositionI,
                Time = endTime,
                Flags = ClimbFlags.Move,
            }
        );

        climbKnots.Add(
            new ClimbKnot
            {
                Position = nextPosition,
                Rotation = lastKnot.Rotation,
                Time = endTime,
                Flags = ClimbFlags.Move,
            }
        );
    }

    private bool HandlePushing(
        ref float time,
        DynamicBuffer<ClimbKnot> climbKnots,
        Entity pushableEntity,
        int3 nextPositionI,
        CellHolder cells,
        int3 nextNextPosition,
        int3 downI,
        ref ClimbKnot lastKnot
    )
    {
        bool push;
        var pushableClimbKnot = SystemAPI.GetBuffer<ClimbKnot>(pushableEntity);
        var pushableMultiPosition = SystemAPI.GetBuffer<MultiPosition>(pushableEntity);
        var lastPushableKnot = pushableClimbKnot[^1];

        var pushableEndPositionI = pushableMultiPosition[^1].Position;

        if (lastPushableKnot.Time - StructConstants.CoyoteTime > time)
        {
            push = false;
        }
        else
        {
            if (lastPushableKnot.Time > time)
            {
                time = lastPushableKnot.Time;
                lastKnot.Time = time;
                climbKnots.Add(lastKnot);
            }

            push = math.all(pushableEndPositionI == nextPositionI) && !cells.IsSolid(nextNextPosition);
        }

        if (push)
        {
            lastPushableKnot.Time = time;
            pushableClimbKnot.Add(lastPushableKnot);

            var blockTime = time + 0.2f;
            pushableClimbKnot.Add(
                new ClimbKnot
                {
                    Position = nextNextPosition,
                    Rotation = lastKnot.Rotation,
                    Time = blockTime,
                    Flags = ClimbFlags.Move,
                }
            );


            cells.SetSolid(nextNextPosition, true);
            cells.SetPushable(nextNextPosition, pushableEntity);

            // Specifically override it instead of adding a new one, because the player will take care
            // of occupying the position later
            cells.SetSolid(nextPositionI, false);
            cells.SetPushable(nextPositionI, Entity.Null);


            pushableMultiPosition[^1] = new MultiPosition
            {
                Position = nextNextPosition,
                Time = blockTime,
                Flags = ClimbFlags.Move,
            };

            SystemAPI.SetComponent(pushableEntity, new Fall { Direction = downI, Duration = 0.04f });
            SystemAPI.SetComponentEnabled<Fall>(pushableEntity, true);
        }

        return push;
    }

    private void QueueJump(
        float time,
        DynamicBuffer<MultiPosition> multiPositions,
        DynamicBuffer<ClimbKnot> climbKnots,
        int3 jumpPositionI,
        ClimbKnot lastKnot,
        ClimbFlags flags
    )
    {
        var jumpTime = time + 0.12f;
        climbKnots.Add(
            new ClimbKnot
            {
                Position = jumpPositionI,
                Rotation = lastKnot.Rotation,
                Time = jumpTime,
                Flags = flags,
            }
        );
        multiPositions.Add(
            new MultiPosition
            {
                Position = jumpPositionI,
                Time = jumpTime,
                Flags = flags,
            }
        );
    }

    private bool CheckPosition(int3 position, DynamicBuffer<MultiPosition> positions)
    {
        var isInList = false;
        for (var i = 0; i < positions.Length; i++) isInList |= math.all(positions[i].Position == position);

        return isInList;
    }

    private void Rotate(DynamicBuffer<RotateKnot> rotateKnots, float multiplier, float time)
    {
        if (rotateKnots.Length > 1) return;

        var firstKnot = rotateKnots[0];
        if (firstKnot.Time + 0.03 > time)
        {
            time = firstKnot.Time;
        }
        else
        {
            firstKnot.Time = time;
            rotateKnots[0] = firstKnot;
        }

        var rotation = quaternion.AxisAngle(new float3(0, 1, 0), math.PIHALF * multiplier);

        rotateKnots.Add(
            new RotateKnot
            {
                Rotation = math.mul(rotation, firstKnot.Rotation),
                Time = time + 0.2f,
            }
        );
    }
}