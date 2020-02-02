using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Burst;

//[AlwaysSynchronizeSystem]
[UpdateAfter(typeof(CollisionEventSystem))]
public class AgentMovement : JobComponentSystem
{
    [BurstCompile]
    protected override void OnCreate()
    {
        /*Entities.ForEach((ref AgentData agData, in LocalToWorld location) =>
        {
            agData.position = new Vector3(location.Position.x, location.Position.y, location.Position.z);
        }).Run();*/
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float dt = Time.DeltaTime;
        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;

        JobHandle job = Entities.WithNativeDisableParallelForRestriction(randomArray).ForEach((int nativeThreadIndex, ref Rotation rot, ref Translation trans , ref AgentData inData) =>
        {
            Vector3 attractor = (inData.destination - inData.position).normalized * inData.speed;
            Vector3 velocity = (inData.direction * inData.speed + attractor * 0.5f + inData.getSteering() * inData.speed *  0.5f).normalized * inData.speed * dt;

            inData.direction = velocity.normalized;
            inData.position += velocity;

            trans.Value.x = trans.Value.x + velocity.x;
            trans.Value.y = trans.Value.y + velocity.y;
            trans.Value.z = trans.Value.z + velocity.z;

            rot.Value = Quaternion.LookRotation(inData.direction);

            inData.setSteering(Vector3.zero);

            if ((inData.destination - inData.position).sqrMagnitude < 0.5f)
            {
                var random = randomArray[nativeThreadIndex];

                inData.destination = new Vector3(random.NextFloat(-50.0f, 50.0f), 1.0f, random.NextFloat(-50.0f, 50.0f));
            }

        }).Schedule(inputDeps);

        return job;
    }

}
