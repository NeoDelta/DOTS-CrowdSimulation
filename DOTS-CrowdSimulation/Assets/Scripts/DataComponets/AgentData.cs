using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct AgentData : IComponentData
{
    public Vector3 position;
    public Vector3 destination;
    public Vector3 direction;
    public float maxSpeed;
    public float speed;
    public float acceleration;

    private Vector3 avoidanceForces;

    public void addAvoidanceForce(Vector3 pos, Vector3 dir)
    {
        Vector3 avoidance = CalculateAgentAvoidanceForce(pos, dir);

        this.avoidanceForces += avoidance.normalized;
    }
    public void setAvoidanceForces(Vector3 steer)
    {
        this.avoidanceForces = steer;
    }

    public Vector3 getAvoidanceForces()
    {
        return this.avoidanceForces;
    }

    private Vector3 CalculateAgentAvoidanceForce(Vector3 pos, Vector3 dir)
    {
        Vector3 velocity = direction.normalized * speed;
        Vector3 tanForce = Vector3.zero;
        Vector3 distance = pos - position;

        tanForce = Vector3.Cross(distance, velocity);
        tanForce = Vector3.Cross(tanForce, distance).normalized;

        //Right bias
        float lambda = 0.5f;
        /*if (density >= 0.7f) lambda = 0.1f;
        else lambda = 0.5f;*/

        if (Mathf.Abs(Vector3.Dot(velocity.normalized, dir.normalized)) <= lambda)
        {

            Vector3 rightForce = new Vector3(velocity.z, velocity.y, -velocity.x);
            tanForce += rightForce * 0.2f;
            tanForce = tanForce.normalized;

        }

        //tanForce *= Mathf.Pow(distance.sqrMagnitude - this.transform.Find("InfluenceBox").GetComponent<BoxCollider>().size.z, 2.0f);
        tanForce *= Mathf.Pow(distance.magnitude - 5.0f, 2.0f);

        if (Vector3.Dot(velocity, dir) > 0)
            tanForce *= 1.2f;
        else
            tanForce *= 2.4f;


        return tanForce;
    }
}
