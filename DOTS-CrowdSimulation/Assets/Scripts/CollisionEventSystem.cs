using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;

public struct TriggerStayRef : IBufferElementData
{
    public Vector3 pos;
    public Vector3 vel;
}

[UpdateAfter(typeof(StepPhysicsWorld)), UpdateAfter(typeof(EndFramePhysicsSystem))]
public class CollisionEventSystem : JobComponentSystem
{
    [BurstCompile, RequireComponentTag(typeof(TriggerStayRef))]
    private struct ClearTriggers : IJobForEachWithEntity<PhysicsVelocity>
    {
        [NativeDisableParallelForRestriction] public BufferFromEntity<TriggerStayRef> TriggerStayRefsFromEntity;

        public void Execute(Entity entity, int index, [ReadOnly] ref PhysicsVelocity velocity)
        {
            TriggerStayRefsFromEntity[entity].Clear();
        }
    }

    private BuildPhysicsWorld buildPhysicsWorldSystem;
    private StepPhysicsWorld stepPhysicsWorld;
    private EndFramePhysicsSystem endFramePhysicsSystem;
    private EntityQuery group;

    protected override void OnCreate()
    {
        buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        endFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
    }

    //[BurstCompile]
    struct CollisionEventSystemJob : ITriggerEventsJob
    {
        [NativeDisableParallelForRestriction] public BufferFromEntity<TriggerStayRef> TriggerStayRefsFromEntity;
        public ComponentDataFromEntity<AgentData> agentData;
        
        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.Entities.EntityA;
            var entityB = triggerEvent.Entities.EntityB;

            if (agentData.Exists(entityA) && agentData.Exists(entityB))
            {
                //Debug.Log($"collision event: {triggerEvent}. Entities: {entityA}, {entityB}");

                //Vector3 stA = agentData[entityA].addSteering(agentData[entityB].position, agentData[entityB].direction * agentData[entityB].speed);
                // Vector3 stB = agentData[entityB].addSteering(agentData[entityA].position, agentData[entityA].direction * agentData[entityA].speed);

                AgentData adA = agentData[entityA];
                /*adA.addSteering(agentData[entityB].position, agentData[entityB].direction * agentData[entityB].speed);
                agentData[entityA] = adA;*/

                AgentData adB = agentData[entityB];
                /*adB.addSteering(agentData[entityA].position, agentData[entityA].direction * agentData[entityA].speed);
                agentData[entityB] = adB;

                /*Debug.Log($"StA: {stA}");
                agentData[entityA].steering = new Vector3(stA.x, stA.y, stA.z);*/
                //Debug.Log($"Steer2: {agentData[entityA].steering}");

                if(TriggerStayRefsFromEntity.Exists(entityA))
                    TriggerStayRefsFromEntity[entityA].Add(new TriggerStayRef { pos = adB.position, vel = adB.direction * adB.speed});
                //if (TriggerStayRefsFromEntity.Exists(entityB))
                    //TriggerStayRefsFromEntity[entityB].Add(new TriggerStayRef { pos = adA.position, vel = adA.direction * adA.speed });


            }  
            
        }


    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var triggerStayRefsFromEntity = GetBufferFromEntity<TriggerStayRef>();
        
        var job1 = new ClearTriggers 
        { 
            TriggerStayRefsFromEntity = triggerStayRefsFromEntity 
        }.Schedule(this, inputDependencies);

        inputDependencies = JobHandle.CombineDependencies(inputDependencies, job1, stepPhysicsWorld.FinalSimulationJobHandle);

        var job2 = new CollisionEventSystemJob()
        {
            TriggerStayRefsFromEntity = triggerStayRefsFromEntity,
            agentData = GetComponentDataFromEntity<AgentData>()
        }.Schedule( stepPhysicsWorld.Simulation, ref buildPhysicsWorldSystem.PhysicsWorld, inputDependencies);

        inputDependencies = JobHandle.CombineDependencies(inputDependencies, job2);

        endFramePhysicsSystem.HandlesToWaitFor.Add(inputDependencies);

        return inputDependencies;
    }
}
