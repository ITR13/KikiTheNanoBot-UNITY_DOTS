using Data;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial struct DestroyOnAudioCompleteSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_, entity) in SystemAPI.Query<DestroyOnAudioCompleteTag>().WithEntityAccess())
            {
                var audioSource = SystemAPI.ManagedAPI.GetComponent<AudioSource>(entity);
                if (!audioSource.isPlaying) ecb.DestroyEntity(entity);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}