﻿using Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Authoring
{
    [ExecuteAlways]
    public class RoomAuthoring : MonoBehaviour
    {
        public Color BaseColor, EdgeColor, AmbientColor, GoalColor, GearColor, WireLightColor;

        private void Update()
        {
            transform.position = math.floor(transform.localScale) / 2f - new float3(0.5f, 0.5f, 0.5f);
        }

        public class RoomAuthoringBaker : Baker<RoomAuthoring>
        {
            public override void Bake(RoomAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.ManualOverride);
                var size = math.round(authoring.transform.localScale);
                var sizeI = (int3)size;

                AddComponent(
                    entity,
                    new Room
                    {
                        Bounds = sizeI,
                    }
                );
                AddComponent(
                    entity,
                    new LocalToWorld
                    {
                        Value = float4x4.TRS(size / 2f - new float3(0.5f, 0.5f, 0.5f), Quaternion.identity, size),
                    }
                );

                AddComponent(
                    entity,
                    new RoomColors
                    {
                        BaseColor = authoring.BaseColor,
                        EdgeColor = authoring.EdgeColor,
                        GearColor = authoring.GearColor,
                        GoalColor = authoring.GoalColor,
                        AmbientColor = authoring.AmbientColor,
                        WireLightColor = authoring.WireLightColor,
                    }
                );
            }
        }
    }
}