using Unity.Entities;
using UnityEngine;

namespace Enums
{
    public struct ShaderProperties : IComponentData
    {
        public int GoalAlpha;
        public int WireColor;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct ShaderPropertiesBakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<ShaderProperties>()) return;
            state.EntityManager.CreateSingleton(
                new ShaderProperties
                {
                    GoalAlpha = Shader.PropertyToID("_GoalAlpha"),
                    WireColor = Shader.PropertyToID("_WireColor"),
                }
            );
        }
    }
}