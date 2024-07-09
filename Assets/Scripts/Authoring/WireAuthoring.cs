using Data;
using Unity.Entities;
using UnityEngine;

public class WireAuthoring : MonoBehaviour
{
    public class WireAuthoringBaker : Baker<WireAuthoring>
    {
        public override void Bake(WireAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<WireTag>(entity);
        }
    }
}