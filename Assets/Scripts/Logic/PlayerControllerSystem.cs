using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[BurstCompile]
[UpdateInGroup(typeof(ControlSystemGroup))]
[UpdateBefore(typeof(FallSystem))]
partial class PlayerControllerSystem : SystemBase
{
    private InputAction _move, _look, _shoot, _jump, _push;

    protected override void OnCreate()
    {
        RequireForUpdate<Player>();
        RequireForUpdate<InputActionsHolder>();
        RequireForUpdate<CellHolder>();
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
        var direction = new int2(_move.ReadValue<Vector2>());
        var push = _push.IsPressed();

        UpdateBursted(direction, push);
    }

    [BurstCompile]
    private void UpdateBursted(in float2 direction, bool push)
    {
        CompleteDependency();
        var player = SystemAPI.GetSingletonEntity<Player>();
        var multiPositions = SystemAPI.GetBuffer<MultiPosition>(player);
        var climbKnots = SystemAPI.GetBuffer<ClimbKnot>(player);
        var rotateKnots = SystemAPI.GetBuffer<RotateKnot>(player);
        var time = (float)SystemAPI.Time.ElapsedTime;


        Climb(player, multiPositions, climbKnots, rotateKnots, math.sign(direction.y), push, time);

        if (direction.x != 0)
        {
            Rotate(rotateKnots, math.sign(direction.x), time);
        }
    }

    private void Climb(
        Entity playerEntity,
        DynamicBuffer<MultiPosition> multiPositions,
        DynamicBuffer<ClimbKnot> climbKnots,
        DynamicBuffer<RotateKnot> rotateKnots,
        float multiplier,
        bool push,
        float time
    )
    {

        var lastKnot = climbKnots[^1];
        if (lastKnot.Time - StructConstants.CoyoteTime > time) return;

        var lastRotate = rotateKnots[^1].Rotation;
        if (lastKnot.Time + StructConstants.CoyoteTime > time)
        {
            time = lastKnot.Time;
        }
        else
        {
            lastKnot.Time = time;
            climbKnots[^1] = lastKnot;
        }

        ref var cells = ref SystemAPI.GetSingletonRW<CellHolder>().ValueRW;

        var rotation = math.mul(lastKnot.Rotation, lastRotate.value);
        var down = math.round(math.mul(rotation, new float3(0, -1, 0)));
        var downI = (int3)down;

        var belowUs = math.round(lastKnot.Position + down);
        var belowUsI = (int3)belowUs;
        if (!cells.IsSolid(belowUsI))
        {
            var temp = SystemAPI.IsComponentEnabled<Fall>(playerEntity);
            if (SystemAPI.IsComponentEnabled<Fall>(playerEntity)) return;

            SystemAPI.SetComponent(playerEntity, new Fall { Direction = downI, Duration = 0.12f });
            SystemAPI.SetComponentEnabled<Fall>(playerEntity, true);
            return;
        }
        SystemAPI.SetComponentEnabled<Fall>(playerEntity, false);

        if (multiplier == 0) return;

        var moveDirection = math.round(math.forward(rotation)) * multiplier;
        var nextPosition = math.round(lastKnot.Position + moveDirection);

        var nextPositionI = (int3)nextPosition;
        var nextIsOwn = CheckPosition(nextPositionI, multiPositions);

        var nextNextPosition = nextPositionI + (int3)moveDirection;

        if (!nextIsOwn && push && cells.IsPushable(nextPositionI, out var pushableEntity))
        {
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
                };

                SystemAPI.SetComponent(pushableEntity, new Fall { Direction = downI, Duration = 0.04f });
                SystemAPI.SetComponentEnabled<Fall>(pushableEntity, true);
            }
        }
        else
        {
            push = false;
        }

        if (!nextIsOwn && !push && cells.IsSolid(nextPositionI))
        {
            var left = math.round(math.mul(rotation, new float3(-1, 0, 0)));
            var climbUpRotation = quaternion.AxisAngle(left, math.PIHALF * multiplier);
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = lastKnot.Position,
                    Rotation = math.mul(climbUpRotation, lastKnot.Rotation),
                    Time = time + 0.2f,
                }
            );
            return;
        }

        cells.SetSolid(nextPositionI, true);
        cells.SetPushable(nextPositionI, playerEntity);

        var posUnderNextI = nextPositionI + downI;
        var posUnderNextIsOwn = CheckPosition(posUnderNextI, multiPositions);

        if (!posUnderNextIsOwn && cells.IsSolid(posUnderNextI))
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
                }
            );

            climbKnots.Add(
                new ClimbKnot
                {
                    Position = nextPosition,
                    Rotation = lastKnot.Rotation,
                    Time = endTime,
                }
            );
            return;
        }

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
                }
            );
            multiPositions.Add(
                new MultiPosition
                {
                    Position = posUnderNextI,
                    Time = endTime,
                }
            );
            cells.SetSolid(posUnderNextI, true);
            cells.SetPushable(posUnderNextI, playerEntity);


            var right = math.round(math.mul(rotation, new float3(1, 0, 0)));
            var climbDownRotation = quaternion.AxisAngle(right, math.PIHALF * multiplier);
            var endRotation = math.mul(climbDownRotation, lastKnot.Rotation);

            climbKnots.Add(
                new ClimbKnot
                {
                    Position = lastKnot.Position + moveDirection * 0.5f,
                    Rotation = lastKnot.Rotation,
                    Time = time + 0.2f * 0.5f,
                }
            );
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = nextPosition + down * 0.5f,
                    Rotation = endRotation,
                    Time = time + 0.8f * 0.5f,
                }
            );
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = posUnderNextI,
                    Rotation = endRotation,
                    Time = endTime,
                }
            );
        }
    }

    private bool CheckPosition(int3 position, DynamicBuffer<MultiPosition> positions)
    {
        var isInList = false;
        for (var i = 0; i < positions.Length; i++)
        {
            isInList |= math.all(positions[i].Position == position);
        }

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
            new RotateKnot()
            {
                Rotation = math.mul(rotation, firstKnot.Rotation),
                Time = time + 0.2f,
            }
        );
    }
}