﻿using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Logic
{
    [UpdateInGroup(typeof(ControlSystemGroup))]
    [UpdateBefore(typeof(ShootSystem))]
    public partial struct BulletSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<CellHolder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var cells = SystemAPI.GetSingleton<CellHolder>();

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var hitPositions = new NativeHashSet<int3>(10, state.WorldUpdateAllocator);

            foreach (var (bullet, entity) in SystemAPI.Query<Bullet>().WithEntityAccess())
            {
                var position = (int3)math.round(bullet.Start + bullet.Forward * (time - bullet.StartTime) * Bullet.Speed);
                if (!cells.IsSolid(position)) continue;
                ecb.DestroyEntity(entity);
                hitPositions.Add(position);
            }

            if (hitPositions.Count <= 0) return;
            foreach (var (target, transform) in SystemAPI.Query<EnabledRefRW<DisabledSwitchTag>, LocalToWorld>())
            {
                var position = (int3)math.round(transform.Position);
                target.ValueRW ^= hitPositions.Contains(position);
            }
        }
    }
}