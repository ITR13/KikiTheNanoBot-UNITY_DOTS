using Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace Authoring
{
    internal class SingletonAuthoring : MonoBehaviour
    {
        public InputActionAsset InputActionAsset;

        public VariablesGroupAsset Variables;

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
                new InputComponent
                {
                    InputActions = authoring.InputActionAsset,
                }
            );

            AddComponent<VariableHolder>(entity, new VariableHolder { VariableGroup = authoring.Variables });

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