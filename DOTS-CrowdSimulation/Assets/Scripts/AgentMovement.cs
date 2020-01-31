using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
[UpdateAfter(typeof(AgentSteering))]
[UpdateAfter(typeof(CollisionEventSystem))]
public class AgentMovement : JobComponentSystem
{
    protected override void OnCreate()
    {
        Entities.ForEach((ref AgentData agData) =>
        {
            agData.setSteering(Vector3.zero);
        }).Run();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float dt = Time.DeltaTime;

        Entities.ForEach((ref Translation trans , ref AgentData inData) =>
        {
            Vector3 velocity = (inData.direction.normalized * 0.1f + inData.getSteering().normalized * 0.9f).normalized * inData.speed * dt;

            inData.position += velocity;

            trans.Value.x = trans.Value.x + velocity.x;
            trans.Value.y = trans.Value.y + velocity.y;
            trans.Value.z = trans.Value.z + velocity.z;

            inData.setSteering(Vector3.zero);
        }).Run();

        return default;
    }

}
