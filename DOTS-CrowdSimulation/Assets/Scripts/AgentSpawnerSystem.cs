using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Mathematics;

public class AgentSpawnerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, AgentSpawnerData agentSpawner) =>
        {
            for (int x = 0; x < agentSpawner.rows; x++)
            {
                float posX = 2.0f*x - 2.0f*agentSpawner.rows + agentSpawner.offset.x;

                for (int z = 0; z < agentSpawner.columns; z++)
                {
                    float posZ = 2.0f*z - 2.0f*agentSpawner.columns + agentSpawner.offset.z;

                    var agentEntity = EntityManager.Instantiate(agentSpawner.AgentPrefabEntity);

                    EntityManager.SetComponentData(agentEntity, new Translation { Value = new float3(posX, 1.0f, posZ) });
                    EntityManager.SetComponentData(agentEntity, new AgentData {
                        position = new Vector3(posX, 1.0f, posZ),
                        destination = new Vector3(UnityEngine.Random.Range(-400.0f, 400.0f), 1.0f, UnityEngine.Random.Range(-400.0f, 400.0f)),
                        direction = new Vector3(0.0f, 0.0f, 0.0f),
                        maxSpeed = UnityEngine.Random.Range(agentSpawner.minSpeed, agentSpawner.maxSpeed),
                        speed = 0.0f,
                        acceleration = 50.0f,
                        hasPath = false,
                        navMeshNodeIndex = -1,
                        pathIndex = 0
                    });
                    EntityManager.AddBuffer<TriggerStayRef>(agentEntity);
                    EntityManager.AddBuffer<PathBuffer>(agentEntity);
                }
            }

            //Dispose of it since where are not going to use it any longer
            EntityManager.DestroyEntity(entity);

        });

    }

}
