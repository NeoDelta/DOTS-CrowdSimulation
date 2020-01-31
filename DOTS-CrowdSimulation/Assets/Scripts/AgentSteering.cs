using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public class AgentSteering : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Entities.ForEach((ref AgentData agData) =>
        {
            //agData.direction = (agData.destination - agData.position).normalized;
        }).Run();
        return default;
    }
}
