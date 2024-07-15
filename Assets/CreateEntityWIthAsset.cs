using Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

public class CreateEntityWIthAsset : MonoBehaviour
{
    [SerializeField] private VariablesGroupAsset Asset;

    void Start()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.CreateSingleton(
            new VariableHolder
            {
                VariableGroup = Asset,
            }
        );
    }
}