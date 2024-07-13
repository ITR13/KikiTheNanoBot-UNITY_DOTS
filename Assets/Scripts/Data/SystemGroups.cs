using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using UnityEngine.Rendering;

namespace Data
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
    public partial class ControlSystemGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
    public partial class RenderSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            RateManager = new RenderRateManager();
            base.OnCreate();
        }
    }

    public unsafe class RenderRateManager : IRateManager
    {
        /// <summary>
        /// Ignored
        /// </summary>
        public float Timestep { get; set; }

        private double _lastElapsedTime;
        private bool _didPushTime;

        /// <summary>
        /// Double rewindable allocators to remember before pushing in rate group allocators.
        /// </summary>
        private DoubleRewindableAllocators* _oldGroupAllocators = null;

        /// <inheritdoc cref="IRateManager.ShouldGroupUpdate"/>
        public bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            // if this is true, means we're being called a second or later time in a loop
            if (_didPushTime)
            {
                group.World.PopTime();
                group.World.RestoreGroupAllocator(_oldGroupAllocators);
                _didPushTime = false;
                return false;
            }

            if (!OnDemandRendering.willCurrentFrameRender)
            {
                _didPushTime = false;
                return false;
            }

            var elapsedTime = group.World.Time.ElapsedTime;
            var deltaTime = (float)(elapsedTime - _lastElapsedTime);

            group.World.PushTime(
                new TimeData(
                    elapsedTime: elapsedTime,
                    deltaTime: deltaTime
                )
            );

            _didPushTime = true;
            _lastElapsedTime += deltaTime;

            _oldGroupAllocators = group.World.CurrentGroupAllocators;
            group.World.SetGroupAllocator(group.RateGroupAllocators);
            return true;
        }
    }
}