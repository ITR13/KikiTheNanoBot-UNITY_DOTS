using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class WheelAuthoring : MonoBehaviour
    {
        public class WheelAuthoringBaker : Baker<WheelAuthoring>
        {
            public override void Bake(WheelAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(
                    entity,
                    new Wheel
                    {
                        IsLeftWheel = authoring.transform.localPosition.x < 0,
                    }
                );
            }
        }
    }
}