using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(VariableRateSimulationSystemGroup))]
    public partial struct AudioIfPoweredSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CellHolder>();
            state.EntityManager.CreateSingleton(
                new ToPlayHolder
                {
                    ToPlay = new NativeList<(UnityObjectRef<AudioClip>, Entity)>(Allocator.Persistent),
                }
            );
        }

        public void OnDestroy(ref SystemState state)
        {
            var toPlay = SystemAPI.GetSingleton<ToPlayHolder>().ToPlay;
            toPlay.Dispose();
        }

        private struct ToPlayHolder : IComponentData
        {
            public NativeList<(UnityObjectRef<AudioClip>, Entity)> ToPlay;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // ReSharper disable once Unity.Entities.SingletonMustBeRequested
            var toPlay = SystemAPI.GetSingleton<ToPlayHolder>().ToPlay;

            foreach (var (audio, gear, entity) in SystemAPI.Query<RefRW<AudioIfPowered>, Gear>()
                         .WithAbsent<Goal>()
                         .WithEntityAccess())
            {
                var isPowered = gear.Powered;
                if (isPowered == audio.ValueRO.WasPowered) continue;
                audio.ValueRW.WasPowered = isPowered;

                var clip = isPowered ? audio.ValueRO.OnSound : audio.ValueRO.OffSound;
                if (!clip.IsValid()) return;

                toPlay.Add((clip, entity));
            }

            foreach (var (audio, switchEnabled, entity) in SystemAPI
                         .Query<RefRW<AudioIfPowered>, EnabledRefRO<DisabledSwitchTag>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                         .WithEntityAccess())
            {
                var isPowered = !switchEnabled.ValueRO;
                if (isPowered == audio.ValueRO.WasPowered) continue;
                audio.ValueRW.WasPowered = isPowered;

                var clip = isPowered ? audio.ValueRO.OnSound : audio.ValueRO.OffSound;
                if (!clip.IsValid()) return;

                toPlay.Add((clip, entity));
            }

            foreach (var (audio, goal, entity) in SystemAPI.Query<RefRW<AudioIfPowered>, Goal>().WithEntityAccess())
            {
                var isPowered = goal.Active;
                if (isPowered == audio.ValueRO.WasPowered) continue;
                audio.ValueRW.WasPowered = isPowered;

                var clip = isPowered ? audio.ValueRO.OnSound : audio.ValueRO.OffSound;
                if (!clip.IsValid()) return;

                toPlay.Add((clip, entity));
            }
        }

        [UpdateInGroup(typeof(RenderSystemGroup))]
        [UpdateAfter(typeof(ActuallyPlayAudioSystem))]
        private partial struct ActuallyPlayAudioSystem : ISystem
        {
            public void OnCreate(ref SystemState state)
            {
                state.RequireForUpdate<ToPlayHolder>();
            }

            public void OnUpdate(ref SystemState state)
            {
                var toPlay = SystemAPI.GetSingleton<ToPlayHolder>().ToPlay;
                foreach (var (clip, entity) in toPlay)
                {
                    var audioSource = SystemAPI.ManagedAPI.GetComponent<AudioSource>(entity);
                    audioSource.PlayOneShot(clip);
                }

                toPlay.Clear();
            }
        }
    }
}