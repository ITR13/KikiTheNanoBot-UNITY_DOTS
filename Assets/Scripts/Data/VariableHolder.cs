using Unity.Entities;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace Data
{
    public struct VariableHolder : IComponentData
    {
        public UnityObjectRef<VariablesGroupAsset> VariableGroup;
    }
}