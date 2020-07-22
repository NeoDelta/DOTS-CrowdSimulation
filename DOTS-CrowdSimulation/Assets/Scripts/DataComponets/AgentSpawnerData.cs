using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class AgentSpawnerData : IComponentData
{
    public Vector3 offset;
    public int rows;
    public int columns;
    public float minSpeed;
    public float maxSpeed;
    public Entity AgentPrefabEntity;
}
