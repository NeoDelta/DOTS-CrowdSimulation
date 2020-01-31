using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
[UpdateAfter(typeof(CollisionEventSystem))]
public class AgentMovement : JobComponentSystem
{
    /*protected override void OnCreate()
    {
        Entities.ForEach((ref AgentData agData) =>
        {
            agData.setSteering(Vector3.zero);
        }).Run();
    }*/

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float dt = Time.DeltaTime;

        /*JobHandle job =*/ Entities.ForEach((ref Translation trans , ref AgentData inData) =>
        {
            Vector3 attractor = (inData.destination - inData.position).normalized * inData.speed;
            Vector3 velocity = (inData.direction * inData.speed + attractor * 0.5f + inData.getSteering() * inData.speed *  0.5f).normalized * inData.speed * dt;

            inData.direction = velocity.normalized;
            inData.position += velocity;

            trans.Value.x = trans.Value.x + velocity.x;
            trans.Value.y = trans.Value.y + velocity.y;
            trans.Value.z = trans.Value.z + velocity.z;

            inData.setSteering(Vector3.zero);
        }).Run();/*.Schedule(inputDeps);*/

        return default;
    }

}
