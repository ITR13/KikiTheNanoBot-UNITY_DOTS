using System;
using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class ObjectAuthoring : MonoBehaviour
{
    public bool Pushable;

    public bool Gear;
    public bool GearGenerator;
    public bool GearToWire;

public bool WireCube;

    public class ObjectAuthoringBaker : Baker<ObjectAuthoring>
    {
        public override void Bake(ObjectAuthoring authoring)
        {
            var transformUsageFlags = authoring.Pushable ? TransformUsageFlags.Dynamic : TransformUsageFlags.Renderable;

            var entity = GetEntity(transformUsageFlags);

            AddComponent<SolidTag>(entity);

            var position = (float3)authoring.transform.position;

            if (authoring.Pushable || authoring.Gear || authoring.WireCube)
            {
                var multiPositions = AddBuffer<MultiPosition>(entity);
                multiPositions.Add(
                    new MultiPosition
                    {
                        Position = (int3)math.round(position),
                        Time = 0,
                    }
                );
            }

            var generator = authoring.Gear && authoring.GearGenerator;
            var gearToWire = authoring.Gear && authoring.GearToWire;

            if (authoring.Gear) AddComponent<Gear>(entity);
            if (generator) AddComponent<GeneratorTag>(entity);
            if (gearToWire) AddComponent<GearToWireTag>(entity);
            
            if(authoring.WireCube) AddComponent<WireCube>(entity);

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