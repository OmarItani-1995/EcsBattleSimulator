using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor |
                   WorldSystemFilterFlags.ThinClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateAfter(typeof(WorldSystemGroup))]
public partial class UnitUpdateSystemGroup : ComponentSystemGroup
{
}
