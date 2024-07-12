using Data;
using Enums;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GoalAuthoring : MonoBehaviour
{
    public bool RequirePower;

    public class GoalAuthoringBaker : Baker<GoalAuthoring>
    {
        public override void Bake(GoalAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            var position = (int3)math.round(authoring.transform.position);
            AddComponent(
                entity,
                new Goal
                {
                    // Goal alpha is set to false on scene load, so it needs to trigger a "change" to update
                    Active = false,
                    WinAtTime = float.PositiveInfinity,
                    Position = position,
                }
            );

            if (authoring.RequirePower)
            {
                AddComponent<WireCube>(entity);
                var buffer = AddBuffer<MultiPosition>(entity);
                buffer.Add(
                    new MultiPosition
                    {
                        Time = 0,
                        Position = position,
                        Flags = ClimbFlags.None,
                    }
                );
            }
        }
    }
}