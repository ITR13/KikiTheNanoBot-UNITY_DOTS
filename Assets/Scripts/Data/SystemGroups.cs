using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class ControlSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ControlSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Presentation)]
public partial class RenderSystemGroup : ComponentSystemGroup
{
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ControlSystemGroup))]
[UpdateBefore(typeof(RenderSystemGroup))]
public partial class EndTickSystemGroup : ComponentSystemGroup
{
}