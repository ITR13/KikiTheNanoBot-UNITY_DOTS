﻿using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Logic
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateAfter(typeof(ShootSystem))]
    public partial struct BulletSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<CellHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var cells = SystemAPI.GetSingleton<CellHolder>();

            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var playerPositions = SystemAPI.GetBuffer<MultiPosition>(playerEntity);
            var ignorePositions = new NativeHashSet<int3>(playerPositions.Length, state.WorldUpdateAllocator);

            foreach (var knot in playerPositions) ignorePositions.Add(knot.Position);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var hitPositions = new NativeHashSet<int3>(10, state.WorldUpdateAllocator);

            foreach (var (bullet, audio, entity) in SystemAPI.Query<Bullet, OneShotAudioReference>().WithEntityAccess())
            {
                var position = (int3)math.round(
                    bullet.Start + bullet.Forward * (time - bullet.StartTime) * Bullet.Speed
                );
                if (!cells.IsSolid(position) || ignorePositions.Contains(position)) continue;
                var audioEntity = ecb.Instantiate(audio.Entity);
                ecb.SetComponent(audioEntity, new LocalToWorld { Value = float4x4.Translate(position) });

                ecb.DestroyEntity(entity);
                hitPositions.Add(position);
            }

            if (hitPositions.Count <= 0) return;
            foreach (var (target, transform) in SystemAPI.Query<EnabledRefRW<DisabledSwitchTag>, LocalToWorld>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                var position = (int3)math.round(transform.Position);
                target.ValueRW ^= hitPositions.Contains(position);
            }
        }
    }
}