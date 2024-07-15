using Data;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioOneShotAuthoring : MonoBehaviour
    {
        public class AudioOneShotAuthoringBaker : Baker<AudioOneShotAuthoring>
        {
            public override void Bake(AudioOneShotAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DestroyOnAudioCompleteTag());
            }
        }
    }
}