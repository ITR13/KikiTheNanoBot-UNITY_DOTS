using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class BulletAuthoring : MonoBehaviour
    {
        public class BulletAuthoringBaker : Baker<BulletAuthoring>
        {
            public override void Bake(BulletAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Bullet>(entity);
            }
        }
    }
}