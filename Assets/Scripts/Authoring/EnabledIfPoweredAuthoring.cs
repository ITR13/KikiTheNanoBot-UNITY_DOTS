using System.Collections.Generic;
using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class EnabledIfPoweredAuthoring : MonoBehaviour
    {
        public List<GameObject> Objects;

        public class EnabledIfPoweredAuthoringBaker : Baker<EnabledIfPoweredAuthoring>
        {
            public override void Bake(EnabledIfPoweredAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<EnabledIfPowered>(entity);
                foreach (var go in authoring.Objects)
                {
                    buffer.Add(
                        new EnabledIfPowered
                        {
                            Entity = GetEntity(go, TransformUsageFlags.None),
                        }
                    );
                }
            }
        }
    }
}