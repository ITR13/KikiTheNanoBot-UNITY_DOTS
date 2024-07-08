using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

partial class PlayerController : SystemBase
{
    private InputAction _move, _look, _shoot, _jump, _push;

    protected override void OnCreate()
    {
        RequireForUpdate<Player>();
        RequireForUpdate<InputActionsHolder>();
        RequireForUpdate<Room>();
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
        CompleteDependency();
        var player = SystemAPI.GetSingletonEntity<Player>();
        var climbKnots = SystemAPI.GetBuffer<ClimbKnot>(player);
        var rotateKnots = SystemAPI.GetBuffer<RotateKnot>(player);

        if (!_move.IsPressed()) return;
        var direction = _move.ReadValue<Vector2>();
        var time = (float)SystemAPI.Time.ElapsedTime;

        if (direction.y != 0)
        {
            Climb(climbKnots, rotateKnots, math.sign(direction.y), time);
        }

        if (direction.x != 0)
        {
            Rotate(rotateKnots, math.sign(direction.x), time);
        }
    }

    private void Climb(
        DynamicBuffer<ClimbKnot> climbKnots,
        DynamicBuffer<RotateKnot> rotateKnots,
        float multiplier,
        float time
    )
    {
        if (climbKnots.Length > 1) return;

        var lastRotate = rotateKnots[^1].Rotation;
        var firstKnot = climbKnots[0];
        if (firstKnot.Time + 0.03 > time)
        {
            time = firstKnot.Time;
        }
        else
        {
            firstKnot.Time = time;
            climbKnots[0] = firstKnot;
        }

        var rotation = math.mul(firstKnot.Rotation, lastRotate.value);
        var forward = math.round(math.forward(rotation));
        var nextPosition = math.round(firstKnot.Position + forward * multiplier);
        var nextPositionI = (int3)nextPosition;

        if (IsSolid(nextPositionI))
        {
            var right = math.round(math.mul(rotation, new float3(-1, 0, 0)));
            var climbRotation = quaternion.AxisAngle(right, math.PIHALF * multiplier);
            climbKnots.Add(
                new ClimbKnot
                {
                    Position = firstKnot.Position,
                    Rotation = math.mul(climbRotation, firstKnot.Rotation),
                    Time = time + 0.2f,
                }
            );
            return;
        }


        climbKnots.Add(
            new ClimbKnot
            {
                Position = nextPosition,
                Rotation = firstKnot.Rotation,
                Time = time + 0.2f,
            }
        );
    }

    private bool IsSolid(int3 position)
    {
        if (math.any(position < 0)) return true;
        var room = SystemAPI.GetSingleton<Room>();
        return math.any(position >= room.Dimensions);
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