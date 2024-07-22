using Data;
using Unity.Entities;
using UnityEngine;

// ReSharper disable Unity.PreferAddressByIdToGraphicsParams

namespace Logic
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor)]
    public partial struct InitializeColorsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RoomColors>();
        }


        public void OnUpdate(ref SystemState state)
        {
            var colorQuery = SystemAPI.QueryBuilder().WithAll<RoomColors>().Build();
            colorQuery.SetChangedVersionFilter(typeof(RoomColors));
            if (colorQuery.IsEmpty) return;

            var colors = colorQuery.GetSingleton<RoomColors>();

            Shader.SetGlobalColor("_BaseColor", colors.BaseColor);
            Shader.SetGlobalColor("_EdgeColor", colors.EdgeColor);
            Shader.SetGlobalColor("_GearColor", colors.GearColor);
            Shader.SetGlobalColor("_GoalColor", colors.GoalColor);
            Shader.SetGlobalColor("_AmbientColor", colors.AmbientColor);
            Shader.SetGlobalColor("_WireLightColor", colors.WireLightColor);

            Shader.SetGlobalFloat("_PushableAlpha", 0.2f);
        }
    }
}