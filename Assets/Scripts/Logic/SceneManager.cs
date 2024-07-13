using Unity.Burst;
using Unity.Entities;

namespace Logic
{
    public partial struct SceneManager : ISystem
    {
        private bool Loading;


        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}