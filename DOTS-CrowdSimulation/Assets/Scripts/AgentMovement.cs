using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;

//[AlwaysSynchronizeSystem]
[UpdateAfter(typeof(CollisionEventSystem))]
public class AgentMovement : JobComponentSystem
{

    [BurstCompile]
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        float dt = Time.DeltaTime;
        var randomArray = World.GetExistingSystem<RandomSystem>().RandomArray;
        var lookup = GetBufferFromEntity<TriggerStayRef>();

        JobHandle job1 = Entities.WithNativeDisableParallelForRestriction(lookup).ForEach((Entity entity, ref AgentData agentData) =>
        {     
            var buffer = lookup[entity];

            for (int i = 0; i < buffer.Length; i++)
            {
                var p = buffer[i].pos;
                var v = buffer[i].vel;
                agentData.addSteering(p, v);
            }

        }).Schedule(inputDeps);

        inputDeps = JobHandle.CombineDependencies(inputDeps, job1);

        JobHandle job = Entities.WithNativeDisableParallelForRestriction(randomArray)
            .ForEach((int nativeThreadIndex, ref Rotation rot, ref Translation trans , ref AgentData inData, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass) =>
        {
            
            Vector3 attractor = (inData.destination - inData.position).normalized * inData.speed;
            Vector3 velocity = (inData.direction * inData.speed + attractor * 0.4f + inData.getSteering() * inData.speed *  0.6f).normalized * inData.speed * dt;

            inData.direction = velocity.normalized;

            trans.Value.x = trans.Value.x + velocity.x;
            trans.Value.y = 1.0f;
            trans.Value.z = trans.Value.z + velocity.z;

            rot.Value = Quaternion.LookRotation(inData.direction);

            physicsVelocity.Linear.x = velocity.x;
            physicsVelocity.Linear.z = velocity.z;

            physicsMass.InverseInertia[0] = 0f;
            physicsMass.InverseInertia[2] = 0f;

            inData.position = trans.Value;

            inData.setSteering(Vector3.zero);

            if ((inData.destination - inData.position).sqrMagnitude < 1.0f)
            {
                var random = randomArray[nativeThreadIndex];

                inData.destination = new Vector3(random.NextFloat(-50.0f, 50.0f), 1.0f, random.NextFloat(-50.0f, 50.0f));
            }

        }).Schedule(inputDeps);

        inputDeps = JobHandle.CombineDependencies(inputDeps, job);

        inputDeps.Complete();

        return inputDeps;
    }

}
