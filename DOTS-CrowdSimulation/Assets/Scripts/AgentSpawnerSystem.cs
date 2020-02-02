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
                float posX = 2.0f*x - agentSpawner.rows;

                for (int z = 0; z < agentSpawner.columns; z++)
                {
                    float posZ = 2.0f*z - agentSpawner.columns;

                    var agentEntity = EntityManager.Instantiate(agentSpawner.AgentPrefabEntity);

                    EntityManager.SetComponentData(agentEntity, new Translation { Value = new float3(posX, 1.0f, posZ) });
                    EntityManager.SetComponentData(agentEntity, new AgentData {
                        position = new Vector3(posX, 1.0f, posZ),
                        destination = new Vector3(UnityEngine.Random.Range(-50.0f, 50.0f), 1.0f, UnityEngine.Random.Range(-50.0f, 50.0f)),
                        direction = new Vector3(0.0f, 0.0f, 0.0f),
                        speed = UnityEngine.Random.Range(1.0f, 3.0f)
                    });
                }
            }

            EntityManager.DestroyEntity(entity);

        });

    }

}
