using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct AgentData : IComponentData
{
    public Vector3 destination;
    public Vector3 direction;
    public float speed;
}
