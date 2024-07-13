using Unity.Burst;
using UnityEngine;

namespace Constants
{
    public struct ShaderProperties
    {
        public static readonly SharedStatic<ShaderProperties> Instance = SharedStatic<ShaderProperties>.GetOrCreate<ShaderProperties>();
        public int GoalAlpha;
        public int WireColor;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            Instance.Data = new ShaderProperties
            {
                GoalAlpha = Shader.PropertyToID("_GoalAlpha"),
                WireColor = Shader.PropertyToID("_WireColor"),
            };
        }
    }
}