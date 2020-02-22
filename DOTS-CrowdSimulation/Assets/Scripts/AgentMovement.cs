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

        JobHandle job1 = Entities.WithNativeDisableParallelForRestriction(lookup).ForEach(( Entity entity, ref AgentData agentData) =>
        {     
            var buffer = lookup[entity];

            for (int i = 0; i < buffer.Length; i++)
            {
                var p = buffer[i].pos;
                var v = buffer[i].vel;
                agentData.addAvoidanceForce(p, v);
            }

        }).Schedule(inputDeps);

        inputDeps = JobHandle.CombineDependencies(inputDeps, job1);

        JobHandle job = Entities.WithNativeDisableParallelForRestriction(randomArray)
            .ForEach((int nativeThreadIndex, ref Rotation rot, ref Translation trans , ref AgentData inData, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass) =>
        {
            
            Vector3 attractor = (inData.destination - inData.position).normalized * inData.maxSpeed;
            //Vector3 velocity = (inData.direction * inData.speed + attractor * 0.4f + inData.getSteering() * inData.speed *  0.6f).normalized * inData.speed * dt;
            Vector3 steering = inData.direction + attractor.normalized * 0.4f + inData.getAvoidanceForces().normalized * 0.6f;
            steering = (steering * inData.maxSpeed).normalized;

            float newSpeed = inData.speed + inData.acceleration * dt;
            if (newSpeed >= inData.maxSpeed) newSpeed = inData.maxSpeed;

            Vector3 velocity = newSpeed * steering;

            inData.direction = velocity.normalized;
            inData.speed = velocity.magnitude;

            trans.Value.x = trans.Value.x + velocity.x * dt;
            trans.Value.y = 1.0f;
            trans.Value.z = trans.Value.z + velocity.z * dt;

            rot.Value = Quaternion.LookRotation(inData.direction);

            physicsVelocity.Linear.x = velocity.x;
            physicsVelocity.Linear.z = velocity.z;

            physicsMass.InverseInertia[0] = 0f;
            physicsMass.InverseInertia[2] = 0f;

            inData.position = trans.Value;

            //Reset avoidance forces for next iteration.
            inData.setAvoidanceForces(Vector3.zero);

            //Set new destination if the agent is near the current destination.
            if ((inData.destination - inData.position).magnitude < 1.0f)
            {
                var random = randomArray[nativeThreadIndex];

                inData.destination = new Vector3(random.NextFloat(-400.0f, 400.0f), 1.0f, random.NextFloat(-400.0f, 400.0f));
            }

        }).Schedule(inputDeps);

        inputDeps = JobHandle.CombineDependencies(inputDeps, job);

        inputDeps.Complete();

        return inputDeps;
    }

}
