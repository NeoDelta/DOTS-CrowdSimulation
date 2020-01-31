using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class CollisionEventSystem : JobComponentSystem
{


    BuildPhysicsWorld buildPhysicsWorldSystem;
    StepPhysicsWorld stepPhysicsWorld;

    protected override void OnCreate()
    {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    //[BurstCompile]
    struct CollisionEventSystemJob : ITriggerEventsJob
    {
        public ComponentDataFromEntity<AgentData> agentData;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.Entities.EntityA;
            var entityB = triggerEvent.Entities.EntityB;

            if (agentData.Exists(entityA) && agentData.Exists(entityB))
            {
                Debug.Log($"collision event: {triggerEvent}. Entities: {entityA}, {entityB}");

                // Vector3 stA = agentData[entityA].addSteering(agentData[entityB].position, agentData[entityB].direction * agentData[entityB].speed);
                // Vector3 stB = agentData[entityB].addSteering(agentData[entityA].position, agentData[entityA].direction * agentData[entityA].speed);

                AgentData adA = agentData[entityA];
                adA.addSteering(agentData[entityB].position, agentData[entityB].direction * agentData[entityB].speed);
                agentData[entityA] = adA;

                AgentData adB = agentData[entityB];
                adB.addSteering(agentData[entityA].position, agentData[entityA].direction * agentData[entityA].speed);
                agentData[entityB] = adB;

                /*Debug.Log($"StA: {stA}");
                agentData[entityA].steering = new Vector3(stA.x, stA.y, stA.z);*/
                Debug.Log($"Steer2: {agentData[entityA].steering}");
            }  
            
        }


    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var job = new CollisionEventSystemJob()
        {
            agentData = GetComponentDataFromEntity<AgentData>()
        }.Schedule( stepPhysicsWorld.Simulation, ref buildPhysicsWorldSystem.PhysicsWorld,
             inputDependencies);

        return job;
    }
}
