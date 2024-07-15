using Unity.Entities;
using UnityEngine;

namespace Data
{
    public struct AudioIfPowered : IComponentData
    {
        public bool WasPowered;
        public UnityObjectRef<AudioClip> OnSound;
        public UnityObjectRef<AudioClip> OffSound;
    }
}