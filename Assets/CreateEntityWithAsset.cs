using Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public class CreateEntityWithAsset : MonoBehaviour
{
    [SerializeField] private VariablesGroupAsset _asset;

    void Start()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.CreateSingleton(
            new VariableHolder
            {
                VariableGroup = _asset,
            }
        );
    }
}