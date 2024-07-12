using Data;
using Unity.Entities;
using UnityEngine;

public class GoalAuthoring : MonoBehaviour
{
    public class GoalAuthoringBaker : Baker<GoalAuthoring>
    {
        public override void Bake(GoalAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new GoalTag());
        }
    }
}