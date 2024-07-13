using Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Authoring
{
    internal class SingletonAuthoring : MonoBehaviour
    {
        public InputActionAsset InputActionAsset;

#if UNITY_EDITOR
        public Mesh DebugMesh;
        public Material DebugMaterial;
#endif
    }

    internal class SingletonAuthoringBaker : Baker<SingletonAuthoring>
    {
        public override void Bake(SingletonAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(
                entity,
                new InputActionsHolder
                {
                    InputActions = authoring.InputActionAsset,
                }
            );

#if UNITY_EDITOR
            AddComponent(
                entity,
                new DebugHolder
                {
                    Material = authoring.DebugMaterial,
                    Mesh = authoring.DebugMesh,
                }
            );
#endif
        }
    }
}