using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class CustomParentingAuthoring : MonoBehaviour
    {
        public bool FollowPosition;

        public class CustomParentingBaker : Baker<CustomParentingAuthoring>
        {
            public override void Bake(CustomParentingAuthoring authoring)
            {
                if (authoring.FollowPosition)
                {
                    var entity = GetEntity(TransformUsageFlags.WorldSpace | TransformUsageFlags.Dynamic);
                    AddComponent(
                        entity,
                        new FollowPosition
                            { Entity = GetEntity(authoring.transform.parent, TransformUsageFlags.Dynamic) }
                    );
                }
                else
                {
                    var entity = GetEntity(TransformUsageFlags.WorldSpace);
                }
            }
        }
    }
}