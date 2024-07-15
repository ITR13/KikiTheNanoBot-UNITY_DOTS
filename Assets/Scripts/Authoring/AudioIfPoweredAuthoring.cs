using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioIfPoweredAuthoring : MonoBehaviour
    {
        public AudioClip OnSound;
        public AudioClip OffSound;

        public class AudioIfPoweredAuthoringBaker : Baker<AudioIfPoweredAuthoring>
        {
            public override void Bake(AudioIfPoweredAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AudioIfPowered { OnSound = authoring.OnSound, OffSound = authoring.OffSound });
            }
        }
    }
}