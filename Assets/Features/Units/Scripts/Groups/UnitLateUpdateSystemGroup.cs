using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor |
                   WorldSystemFilterFlags.ThinClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateAfter(typeof(UnitUpdateSystemGroup))]
public partial class UnitLateUpdateSystemGroup : ComponentSystemGroup
{
    
}
