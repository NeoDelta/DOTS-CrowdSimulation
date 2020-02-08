using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[RequiresEntityConversion]
public class AgentSpawnerConverter : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public int rows;
    public int columns;
    public float minSpeed;
    public float maxSpeed;
    public GameObject AgentPrefab;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(AgentPrefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var agentPrefabEntity = conversionSystem.GetPrimaryEntity(AgentPrefab);

        var agentSpawnerData = new AgentSpawnerData
        {
            rows = this.rows,
            columns = this.columns,
            minSpeed = this.minSpeed,
            maxSpeed = this.maxSpeed,
            AgentPrefabEntity = agentPrefabEntity
        };

        dstManager.AddComponentData(entity, agentSpawnerData);
    }
}
