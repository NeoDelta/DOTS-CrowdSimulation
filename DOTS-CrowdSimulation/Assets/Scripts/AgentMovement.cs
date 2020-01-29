using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
public class AgentMovement : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float deltaTime = Time.DeltaTime;

        Entities.ForEach((ref Translation trans , in AgentData inData) =>
        {
            Vector3 velocity = inData.direction * inData.speed * deltaTime;
            trans.Value.x = trans.Value.x + velocity.x;
            trans.Value.y = trans.Value.y + velocity.y;
            trans.Value.z = trans.Value.z + velocity.z;
        }).Run();

        return default;
    }

}
