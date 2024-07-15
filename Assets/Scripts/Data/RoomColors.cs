using Unity.Entities;
using UnityEngine;

namespace Data
{
    public struct RoomColors : IComponentData
    {
        public Color BaseColor, EdgeColor, AmbientColor, GoalColor, GearColor, WireLightColor;
    }
}