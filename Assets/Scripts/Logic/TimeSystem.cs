using Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Logic
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TimeSystem : ISystem
    {
        private const int TargetRate =
#if UNITY_EDITOR
            50;
#else
            200;
#endif
        private RefreshRate _oldRefreshRate;

        public void OnCreate(ref SystemState state)
        {
            var fixedStepSimulationSystemGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            fixedStepSimulationSystemGroup.Timestep = 1f / TargetRate;

            var presentationSystemGroup = state.World.GetExistingSystemManaged<PresentationSystemGroup>();
            presentationSystemGroup.RateManager = new RenderRateManager();

            QualitySettings.vSyncCount = 1;
        }

        public void OnUpdate(ref SystemState state)
        {
            var refreshRate = Screen.currentResolution.refreshRateRatio;
            if (refreshRate.numerator == _oldRefreshRate.numerator &&
                refreshRate.denominator == _oldRefreshRate.denominator)
                return;
            _oldRefreshRate = refreshRate;


            var ratio = (double)(refreshRate.denominator * TargetRate) / refreshRate.numerator;
            OnDemandRendering.renderFrameInterval = (int)math.ceil(ratio);
            Debug.Log($"Refresh rate of {refreshRate.denominator} / {refreshRate.numerator} gives ratio of {ratio}");
        }
    }
}