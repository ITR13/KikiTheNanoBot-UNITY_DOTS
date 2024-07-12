using Data;
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
            AddComponent(
                entity,
                new Goal
                {
                    // Goal alpha is set to false on scene load, so it needs to trigger a "change" to update
                    Active = false,
                    WinAtTime = float.PositiveInfinity,
                    Position = (int3)math.round(authoring.transform.position),
                }
            );

            if (authoring.RequirePower)
            {
                AddComponent<WireCube>(entity);
            }
        }
    }
}