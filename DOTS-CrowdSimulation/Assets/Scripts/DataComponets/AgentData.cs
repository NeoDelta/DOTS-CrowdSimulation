using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct AgentData : IComponentData
{   
    [Tooltip("Position of the agent (World space)")]
    public Vector3 position;
    [Tooltip("Current destination of the agent")]
    public Vector3 destination;
    [Tooltip("Current diretion of the agent")]
    public Vector3 direction;
    [Tooltip("Maximum speed the agent can achieve")]
    public float maxSpeed;
    [Tooltip("Current/starting speed of the agent")]
    public float speed;
    [Tooltip("Acceleration of the agent")]
    public float acceleration;

    // Sum of all the current avoidance forces to apply
    private Vector3 avoidanceForces;

    // -------Every variable below here should be set to be private later on-------
    // Indicates wheter the agent has a designated path or not
    public bool hasPath;
    public int navMeshNodeIndex;
    public int pathIndex;

    /// <summary>
    /// Adds an avoidance force to <see cref="avoidanceForces"></see> based on
    /// the position an velocity vector of another agent.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="dir"></param>
    public void addAvoidanceForce(Vector3 pos, Vector3 dir)
    {
        Vector3 avoidance = CalculateAgentAvoidanceForce(pos, dir);

        this.avoidanceForces += avoidance.normalized;
    }

    /// <summary>
    /// Sets directly the avoidance forces to appy (testing purposes)
    /// </summary>
    /// <param name="steer"> The amount of steer/avoidance to apply</param>
    public void setAvoidanceForces(Vector3 steer)
    {
        this.avoidanceForces = steer;
    }

    /// <summary>
    /// Returns <see cref="avoidanceForces"/>
    /// </summary>
    /// <returns></returns>
    public Vector3 getAvoidanceForces()
    {
        return this.avoidanceForces;
    }

    private Vector3 CalcWallAvoidanceForce(Vector3 normal)
    {
        Vector3 tanForce;

        tanForce = Vector3.Cross(normal, direction.normalized * speed);
        tanForce = Vector3.Cross(tanForce, normal).normalized;

        return tanForce;
    }

    /// <summary>
    /// Calculates the avoidance force between the agent and another one.
    /// </summary>
    /// <param name="pos"> Position of the other agent</param>
    /// <param name="dir"> Velocity vector of the other agent</param>
    /// <returns></returns>
    private Vector3 CalculateAgentAvoidanceForce(Vector3 pos, Vector3 dir)
    {
        Vector3 tanForce;
        Vector3 velocity = direction.normalized * speed; 
        Vector3 distance = pos - position;

        tanForce = Vector3.Cross(distance, velocity);
        tanForce = Vector3.Cross(tanForce, distance).normalized;

        //Right bias
        float bias = 0.5f;
        /*if (density >= 0.7f) bias = 0.1f;
        else bias = 0.5f;*/

        if (Mathf.Abs(Vector3.Dot(velocity.normalized, dir.normalized)) <= bias)
        {

            Vector3 rightForce = new Vector3(velocity.z, velocity.y, -velocity.x);
            tanForce += rightForce * 0.2f;
            tanForce = tanForce.normalized;

        }

        //tanForce *= Mathf.Pow(distance.sqrMagnitude - this.transform.Find("InfluenceBox").GetComponent<BoxCollider>().size.z, 2.0f);
        tanForce *= Mathf.Pow(distance.magnitude - 6.0f, 2.0f);

        if (Vector3.Dot(velocity, dir) > 0)
            tanForce *= 1.2f;
        else
            tanForce *= 2.4f;


        return tanForce;
    }

    /// <summary>
    /// Calculates the projection of the agent position to the destination portal (represented as it's to end points).
    /// </summary>
    /// <param name="v1"> End point 1 of the portal.</param>
    /// <param name="v2"> End point 2 of the portal.</param>
    /// <returns></returns>
    public Vector3 getProyectedWayPoint(Vector3 v1, Vector3 v2)
    {
        float x = position.x;
        float y = position.z;
        float x1 = v1.x;
        float y1 = v1.z;
        float x2 = v2.x;
        float y2 = v2.z;

        float A = x - x1;
        float B = y - y1;
        float C = x2 - x1;
        float D = y2 - y1;

        float dot = A * C + B * D;
        float len_sq = C * C + D * D;
        float param = -1;
        if (len_sq != 0) //in case of 0 length line
            param = dot / len_sq;

        float xx, yy;

        if (param < 0)
        {
            xx = x2;
            yy = y2;
        }
        else if (param > 1)
        {
            xx = x1;
            yy = y1;
        }
        else
        {
            xx = x1 + param * C;
            yy = y1 + param * D;
        }
        return new Vector3(xx, position.y, yy);
    }
}
