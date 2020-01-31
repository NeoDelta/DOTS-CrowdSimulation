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
    public float speed;

    public Vector3 steering;

    public Vector3 addSteering(Vector3 pos, Vector3 dir)
    {
        Vector3 avoidance = CalcAgentAvoidanceForce(pos, dir);

        this.steering += avoidance.normalized;
        this.steering = steering.normalized;

        Debug.Log($"Steer: {this.steering}");

        return this.steering;
    }
    public void setSteering(Vector3 steer)
    {
        this.steering = steer;
    }

    public Vector3 getSteering()
    {
        //Debug.Log($"Steer2: {steering.normalized}");
        return this.steering.normalized;
    }

    private Vector3 CalcAgentAvoidanceForce(Vector3 pos, Vector3 dir)
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
            //tanForce = tanForce.normalized;

        }

        //tanForce *= Mathf.Pow(distance.sqrMagnitude - this.transform.Find("InfluenceBox").GetComponent<BoxCollider>().size.z, 2.0f);
        tanForce *= Mathf.Pow(distance.sqrMagnitude - 5.0f, 2.0f);

        if (Vector3.Dot(velocity, dir) > 0)
            tanForce *= 1.2f;
        else
            tanForce *= 2.4f;


        return tanForce;
    }
}
