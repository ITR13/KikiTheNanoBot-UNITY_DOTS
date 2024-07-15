using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Data
{
    public struct CameraBridge : IComponentData
    {
        public bool Spawned;
        public UnityObjectRef<GameObject> Camera;

        public float2 CurrentLookRotation;
    }
}