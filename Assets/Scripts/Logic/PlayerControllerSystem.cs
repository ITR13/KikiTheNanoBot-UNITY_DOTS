using Constants;
using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateBefore(typeof(FallSystem))]
    internal partial struct PlayerControllerSystem : ISystem
    {
        private const float JumpTime = 0.2f;
        private const float MoveTime = 0.2f;
        private const float ClimbUpTime = 0.5f;


        // I'm lazy, sue me
        private bool _hasJumped;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<InputComponent>();
            state.RequireForUpdate<CellHolder>();
            state.RequireForUpdate<Goal>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            var inputs = SystemAPI.GetSingleton<InputComponent>();

            var direction = inputs.Move;
            var push = inputs.Push.CurrentlyPressed;
            var jumpHeld = inputs.Jump.CurrentlyPressed;
            var jumpReleased = inputs.Jump.ReleasedThisFrame && !_hasJumped;

            UpdateBursted(ref state, direction, push, jumpHeld, jumpReleased);
            _hasJumped &= jumpHeld;
        }

        [BurstCompile]
        private void UpdateBursted(
            ref SystemState state,
            in float2 direction,
            bool push,
            bool jumpHeld,
            bool jumpReleased
        )
        {
            var goal = SystemAPI.GetSingleton<Goal>();
            // Goal is positive infinity until we reach it
            if (goal.WinAtTime < double.PositiveInfinity) return;
            var playerEntity = SystemAPI.GetSingletonEntity<Player>();

            var playerComponent = SystemAPI.GetComponent<Player>(playerEntity);
            var multiPositions = SystemAPI.GetBuffer<MultiPosition>(playerEntity);
            var climbKnots = SystemAPI.GetBuffer<ClimbKnot>(playerEntity);


            var time = (float)SystemAPI.Time.ElapsedTime;
            var firstPosition = multiPositions[0].Position;

            if (goal.Active && math.all(firstPosition == goal.Position))
            {
                if (climbKnots.Length > 1)
                {
                    climbKnots.RemoveRange(1, climbKnots.Length - 1);
                }

                SystemAPI.SetComponentEnabled<Fall>(playerEntity, false);
                goal.WinAtTime = time + 1;
                SystemAPI.SetSingleton(goal);

                var audio = state.EntityManager.Instantiate(playerComponent.GoalAudio);
                SystemAPI.SetComponent(
                    audio,
                    new LocalToWorld
                    {
                        Value = float4x4.Translate(firstPosition),
                    }
                );
                return;
            }

            var lastPosition = multiPositions[^1].Position;


            var rotateKnots = SystemAPI.GetBuffer<RotateKnot>(playerEntity);
            var wheelKnots = SystemAPI.GetBuffer<WheelKnot>(playerEntity);

            Climb(
                ref state,
                time,
                playerEntity,
                multiPositions,
                climbKnots,
                rotateKnots,
                wheelKnots,
                math.sign(direction.y),
                push,
                jumpHeld,
                jumpReleased,
                false,
                out var didMove,
                out var didJump,
                out var didPush
            );

            if (direction.x != 0)
            {
                Rotate(ref state, time, rotateKnots, wheelKnots, math.sign(direction.x), out var didRotate);
                didMove |= didRotate;
            }

            if (didPush)
            {
                var audio = state.EntityManager.Instantiate(playerComponent.PushAudio);
                SystemAPI.SetComponent(
                    audio,
                    new LocalToWorld
                    {
                        Value = float4x4.Translate(lastPosition),
                    }
                );
            }

            if (didJump)
            {
                var audio = state.EntityManager.Instantiate(playerComponent.JumpAudio);
                SystemAPI.SetComponent(
                    audio,
                    new LocalToWorld
                    {
                        Value = float4x4.Translate(lastPosition),
                    }
                );
            }
            else if (didMove)
            {
                var audio = state.EntityManager.Instantiate(playerComponent.MoveAudio);
                SystemAPI.SetComponent(
                    audio,
                    new LocalToWorld
                    {
                        Value = float4x4.Translate(lastPosition),
                    }
                );
            }
        }

        private void Climb(
            ref SystemState state,
            float time,
            Entity playerEntity,
            DynamicBuffer<MultiPosition> multiPositions,
            DynamicBuffer<ClimbKnot> climbKnots,
            DynamicBuffer<RotateKnot> rotateKnots,
            DynamicBuffer<WheelKnot> wheelKnots,
            float multiplier,
            bool push,
            bool jumpHeld,
            bool jumpReleased,
            bool fallingForward,
            out bool didMove,
            out bool didJump,
            out bool didPush
        )
        {
            didMove = false;
            didJump = false;
            didPush = false;

            var lastKnot = climbKnots[^1];
            ref var cells = ref SystemAPI.GetSingletonRW<CellHolder>().ValueRW;
            var lastRotate = rotateKnots[^1].Rotation;
            var rotation = math.mul(lastKnot.Rotation, lastRotate.value);
            var moveDirection = math.round(math.forward(rotation)) * multiplier;

            var down = math.round(math.mul(rotation, new float3(0, -1, 0)));
            var downI = (int3)down;

            if (lastKnot.Time - StructConstants.CoyoteTime > time && !fallingForward)
            {
                if (multiplier == 0 || !CanUndoFall(cells, climbKnots, multiPositions, moveDirection, downI))
                {
                    return;
                }

                while (climbKnots.Length > 1 && climbKnots[^1].Flags == ClimbFlags.Fall) climbKnots.Length--;

                while (multiPositions.Length > 1 && multiPositions[^1].Flags == ClimbFlags.Fall)
                {
                    var removedPosition = multiPositions[^1].Position;
                    cells.SetSolid(removedPosition, false);
                    cells.SetPushable(removedPosition, Entity.Null);
                    multiPositions.Length--;
                }

                lastKnot = climbKnots[^1];
            }

            var player = SystemAPI.GetComponent<Player>(playerEntity);
            var nextPosition = math.round(lastKnot.Position + moveDirection);

            var nextPositionI = (int3)nextPosition;
            var nextIsOwn = CheckPosition(nextPositionI, multiPositions);
            var nextIsSolid = !nextIsOwn && cells.IsSolid(nextPositionI);


            if (lastKnot.Time + StructConstants.CoyoteTime > time)
            {
                time = lastKnot.Time;
            }
            else
            {
                lastKnot.Time = time;
                climbKnots[^1] = lastKnot;
            }

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
                didJump = true;

                HandlePlayerCells(playerEntity, multiPositions, aboveIsOwn, jumpPositionI, ref cells);
                QueueJump(ref state, time, multiPositions, climbKnots, jumpPositionI, lastKnot, ClimbFlags.Jump);
                player.Moves += 1;
                SystemAPI.SetComponent(playerEntity, player);
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

                player.Moves += 1;
                SystemAPI.SetComponent(playerEntity, player);

                HandlePlayerCells(playerEntity, multiPositions, isOwn, destination, ref cells);
                QueueJump(ref state, time, multiPositions, climbKnots, destination, lastKnot, flags);
                _hasJumped = true;
                didJump = true;

                if (!jumpUpInstead)
                {
                    // Force move forward :)
                    Climb(
                        ref state,
                        time,
                        playerEntity,
                        multiPositions,
                        climbKnots,
                        rotateKnots,
                        wheelKnots,
                        1,
                        false,
                        false,
                        false,
                        true,
                        out _,
                        out _,
                        out _
                    );
                }

                return;
            }

            var nextNextPosition = nextPositionI + (int3)moveDirection;

            if (isGrounded && !nextIsOwn && push && cells.IsPushable(nextPositionI, out var pushableEntity))
            {
                push = HandlePushing(
                    ref state,
                    ref time,
                    climbKnots,
                    pushableEntity,
                    nextPositionI,
                    cells,
                    nextNextPosition,
                    downI,
                    ref lastKnot
                );
                didPush = push;
            }

            else
            {
                push = false;
            }

            // TODO: If slippery and not grounded, disallow climbing
            if (!push && nextIsSolid)
            {
                QueueClimbUp(
                    time,
                    climbKnots,
                    wheelKnots,
                    multiplier,
                    rotation,
                    lastKnot
                );
                didMove = true;

                // Special case where we climb up something as we jump
                SystemAPI.SetComponentEnabled<Fall>(playerEntity, false);
                return;
            }


            var posUnderNextI = nextPositionI + downI;
            var posUnderNextIsOwn = CheckPosition(posUnderNextI, multiPositions);

            if (!posUnderNextIsOwn && cells.IsSolid(posUnderNextI))
            {
                HandlePlayerCells(playerEntity, multiPositions, nextIsOwn, nextPositionI, ref cells);
                QueueMove(
                    time,
                    multiPositions,
                    climbKnots,
                    wheelKnots,
                    nextPositionI,
                    nextPosition,
                    lastKnot,
                    multiplier
                );
                didMove = true;
                SystemAPI.SetComponentEnabled<Fall>(playerEntity, false);
                player.Moves += 1;
                SystemAPI.SetComponent(playerEntity, player);
                return;
            }

            if (fallingForward)
            {
                HandlePlayerCells(playerEntity, multiPositions, posUnderNextIsOwn, posUnderNextI, ref cells);
                QueueJump(ref state, time, multiPositions, climbKnots, posUnderNextI, lastKnot, ClimbFlags.FallForward);
                player.Moves += 1;
                SystemAPI.SetComponent(playerEntity, player);
                return;
            }

            if (isGrounded)
            {
                didMove = true;
                HandlePlayerCells(playerEntity, multiPositions, nextIsOwn, nextPositionI, ref cells);
                HandlePlayerCells(playerEntity, multiPositions, posUnderNextIsOwn, posUnderNextI, ref cells);
                QueueClimbDown(
                    time,
                    multiPositions,
                    climbKnots,
                    wheelKnots,
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
                player.Moves += 1;
                SystemAPI.SetComponent(playerEntity, player);
            }
        }

        private bool CanUndoFall(
            CellHolder cells,
            DynamicBuffer<ClimbKnot> climbKnots,
            DynamicBuffer<MultiPosition> multiPositions,
            float3 moveDirection,
            int3 downI
        )
        {
            var lastKnot = climbKnots[0];
            for (var i = climbKnots.Length - 1; i > 0; i--)
            {
                if (climbKnots[i].Flags == ClimbFlags.Fall) continue;
                lastKnot = climbKnots[i];
                break;
            }

            if ((lastKnot.Flags & ClimbFlags.Fall) == ClimbFlags.None) return false;

            var nextPosition = math.round(lastKnot.Position + moveDirection);

            var nextPositionI = (int3)nextPosition;
            var nextIsOwn = CheckPosition(nextPositionI, multiPositions);
            var nextIsSolid = !nextIsOwn && cells.IsSolid(nextPositionI);

            if (nextIsSolid) return true;

            var underNextPositionI = nextPositionI + downI;
            var underNextIsOwn = CheckPosition(underNextPositionI, multiPositions);
            var underNextIsSolid = !underNextIsOwn && cells.IsSolid(underNextPositionI);

            return underNextIsSolid;
        }

        private static void QueueClimbUp(
            float time,
            DynamicBuffer<ClimbKnot> climbKnots,
            DynamicBuffer<WheelKnot> wheelKnots,
            float multiplier,
            quaternion rotation,
            ClimbKnot lastKnot
        )
        {
            var left = math.round(math.mul(rotation, new float3(-1, 0, 0)));
            var climbUpRotation = quaternion.AxisAngle(left, math.PIHALF * multiplier);
            var endTime = time + MoveTime;
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = lastKnot.Position,
                    Rotation = math.mul(climbUpRotation, lastKnot.Rotation),
                    Time = endTime,
                    Flags = ClimbFlags.ClimbUp,
                }
            );

            var wheelKnot = wheelKnots[^1];
            wheelKnot.Time = time;
            wheelKnots[^1] = wheelKnot;

            wheelKnots.Add(
                new WheelKnot
                {
                    Time = endTime,
                    LeftRotation = wheelKnot.LeftRotation + math.PIHALF * multiplier,
                    RightRotation = wheelKnot.RightRotation + math.PIHALF * multiplier,
                }
            );
        }

        private static void QueueClimbDown(
            float time,
            DynamicBuffer<MultiPosition> multiPositions,
            DynamicBuffer<ClimbKnot> climbKnots,
            DynamicBuffer<WheelKnot> wheelKnots,
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
            var endTime = time + ClimbUpTime;
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

            var part1Time = time + 0.2f * ClimbUpTime;
            var part2Time = time + 0.8f * ClimbUpTime;

            climbKnots.Add(
                new ClimbKnot
                {
                    Position = lastKnot.Position + moveDirection * 0.5f,
                    Rotation = lastKnot.Rotation,
                    Time = part1Time,
                    Flags = ClimbFlags.ClimbDown,
                }
            );
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = nextPosition + down * 0.5f,
                    Rotation = endRotation,
                    Time = part2Time,
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


            var wheelKnot = wheelKnots[^1];
            wheelKnot.Time = time;
            wheelKnots[^1] = wheelKnot;

            wheelKnots.Add(
                new WheelKnot
                {
                    Time = part1Time,
                    LeftRotation = wheelKnot.LeftRotation + multiplier,
                    RightRotation = wheelKnot.RightRotation + multiplier,
                }
            );
            wheelKnots.Add(
                new WheelKnot
                {
                    Time = part2Time,
                    LeftRotation = wheelKnot.LeftRotation + (1 + math.PIHALF) * multiplier,
                    RightRotation = wheelKnot.RightRotation + (1 + math.PIHALF) * multiplier,
                }
            );
            wheelKnots.Add(
                new WheelKnot
                {
                    Time = endTime,
                    LeftRotation = wheelKnot.LeftRotation + (2 + math.PIHALF) * multiplier,
                    RightRotation = wheelKnot.RightRotation + (2 + math.PIHALF) * multiplier,
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
            DynamicBuffer<WheelKnot> wheelKnots,
            int3 nextPositionI,
            float3 nextPosition,
            ClimbKnot lastKnot,
            float multiplier
        )
        {
            var endTime = time + MoveTime;
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

            var wheelKnot = wheelKnots[^1];
            wheelKnot.Time = time;
            wheelKnots[^1] = wheelKnot;

            wheelKnots.Add(
                new WheelKnot
                {
                    Time = endTime,
                    LeftRotation = wheelKnot.LeftRotation + 2 * multiplier,
                    RightRotation = wheelKnot.RightRotation + 2 * multiplier,
                }
            );
        }

        private bool HandlePushing(
            ref SystemState state,
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

                var blockTime = time + MoveTime;
                pushableClimbKnot.Add(
                    new ClimbKnot
                    {
                        Position = nextNextPosition,
                        Rotation = lastPushableKnot.Rotation,
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

                if (!cells.IsSolid(nextNextPosition + downI))
                {
                    SystemAPI.SetComponent(pushableEntity, new Fall { Direction = downI, Duration = 0.04f });
                    SystemAPI.SetComponentEnabled<Fall>(pushableEntity, true);
                }
            }

            return push;
        }

        private void QueueJump(
            ref SystemState state,
            float time,
            DynamicBuffer<MultiPosition> multiPositions,
            DynamicBuffer<ClimbKnot> climbKnots,
            int3 jumpPositionI,
            ClimbKnot lastKnot,
            ClimbFlags flags
        )
        {
            var jumpTime = time + JumpTime;
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

        private void Rotate(
            ref SystemState state,
            float time,
            DynamicBuffer<RotateKnot> rotateKnots,
            DynamicBuffer<WheelKnot> wheelKnots,
            float multiplier,
            out bool didRotate
        )
        {
            didRotate = rotateKnots.Length <= 1;
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

            var endTime = time + MoveTime;
            rotateKnots.Add(
                new RotateKnot
                {
                    Rotation = math.mul(rotation, firstKnot.Rotation),
                    Time = endTime,
                }
            );

            var wheelKnot = wheelKnots[^1];
            wheelKnot.Time = time;
            wheelKnots[^1] = wheelKnot;

            wheelKnots.Add(
                new WheelKnot
                {
                    Time = endTime,
                    LeftRotation = wheelKnot.LeftRotation + math.PIHALF * multiplier,
                    RightRotation = wheelKnot.RightRotation - math.PIHALF * multiplier,
                }
            );
        }
    }
}