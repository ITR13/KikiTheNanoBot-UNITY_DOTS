using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class CameraBridgeAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        public class CameraBridgeAuthoringBaker : Baker<CameraBridgeAuthoring>
        {
            public override void Bake(CameraBridgeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(
                    entity,
                    new CameraBridge
                    {
                        Spawned = false,
                        Camera = authoring.Prefab,
                    }
                );
            }
        }
    }
}