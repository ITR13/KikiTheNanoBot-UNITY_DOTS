using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class KolbenAuthoring : MonoBehaviour
    {
        public class KolbenTagAuthoringBaker : Baker<KolbenAuthoring>
        {
            public override void Bake(KolbenAuthoring authoring)
            {
                var transform = GetComponent<Transform>();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Kolben { StartPosition = transform.position });
            }
        }
    }
}