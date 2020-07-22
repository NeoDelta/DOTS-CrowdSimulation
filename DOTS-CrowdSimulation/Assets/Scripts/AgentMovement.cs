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

        //Calculate avoidance forces
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

        //Apply forces and calculate new velocity vector
        JobHandle job = Entities.WithNativeDisableParallelForRestriction(randomArray)
            .ForEach((int nativeThreadIndex, ref Rotation rot, ref Translation trans , ref AgentData inData, ref PhysicsVelocity physicsVelocity, ref PhysicsMass physicsMass, ref DynamicBuffer<PathBuffer> pathBuffer) =>
        {
            if (inData.hasPath)
            {
                inData.position = trans.Value;

                if (pathBuffer.Length > 0) inData.destination = inData.getProyectedWayPoint(pathBuffer[inData.pathIndex].v1, pathBuffer[inData.pathIndex].v2);

                Vector3 attractor = (inData.destination - inData.position).normalized * inData.maxSpeed;
                //Vector3 velocity = (inData.direction * inData.speed + attractor * 0.4f + inData.getSteering() * inData.speed *  0.6f).normalized * inData.speed * dt;
                Vector3 currentVelocity = new Vector3(physicsVelocity.Linear.x, 0.0f, physicsVelocity.Linear.z);
                Vector3 steering = currentVelocity + attractor.normalized * 0.3f + inData.getAvoidanceForces().normalized * 0.4f;
                steering = (steering * inData.maxSpeed).normalized;

                float newSpeed = inData.speed + inData.acceleration * dt;
                if (newSpeed >= inData.maxSpeed) newSpeed = inData.maxSpeed;

                Vector3 velocity = newSpeed * steering;

                inData.direction = velocity.normalized;
                inData.speed = velocity.magnitude;

                //trans.Value.x = trans.Value.x + velocity.x * dt;
                trans.Value.y = 0.575f;
                //trans.Value.z = trans.Value.z + velocity.z * dt;

                rot.Value = Quaternion.LookRotation(inData.direction);
                //rot.Value = Quaternion.Lerp(Quaternion.LookRotation(inData.direction), rot.Value, 2f*dt);

                physicsVelocity.Linear.x = velocity.x;
                physicsVelocity.Linear.y = 0.0f;
                physicsVelocity.Linear.z = velocity.z;

                physicsMass.InverseInertia[0] = 0f;
                physicsMass.InverseInertia[2] = 0f;

                //inData.position = trans.Value;

                //Reset avoidance forces for next iteration.
                inData.setAvoidanceForces(Vector3.zero);

                //Set new destination if the agent is near the current destination.
                if ((inData.destination - inData.position).magnitude < 2.0f)
                {
                    var random = randomArray[nativeThreadIndex];

                    //Data.destination = new Vector3(random.NextFloat(-400.0f, 400.0f), 1.0f, random.NextFloat(-400.0f, 400.0f));
                    inData.pathIndex -= 1;
                    if (inData.pathIndex <= 0) inData.hasPath = false;
                    inData.navMeshNodeIndex = pathBuffer[inData.pathIndex].nodeIndex;
                }
            }

        }).Schedule(inputDeps);

        inputDeps = JobHandle.CombineDependencies(inputDeps, job);

        inputDeps.Complete();

        return inputDeps;
    }

}
