using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

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


    [BurstCompile]
    struct CollisionEventSystemJob : ITriggerEventsJob
    {
        [NativeDisableParallelForRestriction] public BufferFromEntity<TriggerStayRef> TriggerStayRefsFromEntity;
        public ComponentDataFromEntity<AgentData> agentData;
        public ComponentDataFromEntity<Translation> translationData;

        public void Execute(TriggerEvent triggerEvent)
        {
            var entityA = triggerEvent.Entities.EntityA;
            var entityB = triggerEvent.Entities.EntityB;


            if (agentData.Exists(entityA) && agentData.Exists(entityB))
            {
                AgentData adA = agentData[entityA];
                AgentData adB = agentData[entityB];

                if(TriggerStayRefsFromEntity.Exists(entityA))
                    TriggerStayRefsFromEntity[entityA].Add(new TriggerStayRef { pos = adB.position, vel = adB.direction * adB.speed});
                //if (TriggerStayRefsFromEntity.Exists(entityB))
                    //TriggerStayRefsFromEntity[entityB].Add(new TriggerStayRef { pos = adA.position, vel = adA.direction * adA.speed });


            }  
            
            if (agentData.Exists(entityA) && !agentData.Exists(entityB) && translationData.Exists(entityB))
            {
                AgentData adA = agentData[entityA];
                Translation trB = translationData[entityB];

                float3 origin = adA.position;
                float3 offset = trB.Value - origin;
            }
        }
    }

    [BurstCompile]
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
            agentData = GetComponentDataFromEntity<AgentData>(),
            translationData = GetComponentDataFromEntity<Translation>()
        }.Schedule( stepPhysicsWorld.Simulation, ref buildPhysicsWorldSystem.PhysicsWorld, inputDependencies);

        inputDependencies = JobHandle.CombineDependencies(inputDependencies, job2);

        endFramePhysicsSystem.HandlesToWaitFor.Add(inputDependencies);

        return inputDependencies;
    }
}
