using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Authoring
{
    [ExecuteAlways]
    public class ObjectAuthoring : MonoBehaviour
    {
        public bool Pushable;

        public bool Gear;
        public bool GearMotor;
        public bool GearToWire;

        public bool WireCube;

        public bool Shootable;

        public class ObjectAuthoringBaker : Baker<ObjectAuthoring>
        {
            public override void Bake(ObjectAuthoring authoring)
            {
                var transformUsageFlags =
                    authoring.Pushable ? TransformUsageFlags.Dynamic : TransformUsageFlags.Renderable;

                var entity = GetEntity(transformUsageFlags);

                AddComponent<SolidTag>(entity);
                if (authoring.Shootable)
                {
                    AddComponent<DisabledSwitchTag>(entity);
                }

                var position = (float3)authoring.transform.position;
                var positionI = (int3)math.round(position);

                if (authoring.Pushable || authoring.Gear || authoring.WireCube)
                {
                    var multiPositions = AddBuffer<MultiPosition>(entity);
                    multiPositions.Add(
                        new MultiPosition
                        {
                            Position = positionI,
                            Time = 0,
                        }
                    );
                }

                var motor = authoring.Gear && authoring.GearMotor;
                var gearToWire = authoring.Gear && authoring.GearToWire;

                if (authoring.Gear)
                {
                    AddComponent<Gear>(entity);
                    AddComponent(
                        entity,
                        new GearSpeed
                        {
                            Speed = ((positionI.x + positionI.z) & 1) * 2 - 1,
                        }
                    );
                }

                if (motor) AddComponent<MotorTag>(entity);
                if (gearToWire) AddComponent<GearToWireTag>(entity);

                if (authoring.WireCube) AddComponent<WireCube>(entity);

                if (!authoring.Pushable) return;

                AddComponent(entity, new PushableTag());

                var climbKnots = AddBuffer<ClimbKnot>(entity);
                climbKnots.Add(
                    new ClimbKnot
                    {
                        Position = position,
                        Rotation = Quaternion.identity,
                        Time = 0,
                    }
                );


                AddComponent<Fall>(entity);
                SetComponentEnabled<Fall>(entity, false);
            }
        }
    }
}