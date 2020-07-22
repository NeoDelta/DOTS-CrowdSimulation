using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System;
using Unity.Collections;

public struct PathBuffer : IBufferElementData
{
    public Vector3 v1;
    public Vector3 v2;
    public int nodeIndex;
}

public class NavMeshSystem : ComponentSystem
{
    private NativeList<Node> nodes;
    private NativeList<Neighbour> neighbours;
    private NativeList<Vector3> meshVertices;
    private NativeList<int> meshIndices;

    private struct Portal
    {
        public Vector3 v1;
        public Vector3 v2;

    }

    private struct Neighbour
    {
        public Portal portal;
        public int nodeIndex;
    }

    struct Node
    {
        public int index;
        public Vector3 center;
        public int verticesIndex;
        public int neighboursIndex;
        public int neighboursSize;
    }

    protected override void OnCreate()
    {
        var navMesh = NavMesh.CalculateTriangulation();

        meshVertices = new NativeList<Vector3>(Allocator.Temp);
        foreach (Vector3 v in navMesh.vertices)
            meshVertices.Add(v);

        meshIndices = new NativeList<int>(Allocator.Temp);
        foreach (int idx in navMesh.indices)
            meshIndices.Add(idx);

        nodes = new NativeList<Node>(Allocator.Persistent);
        neighbours = new NativeList<Neighbour>(Allocator.Persistent);

        // Initialize node lists
        for(int index = 0; index < meshIndices.Length; index+=3)
        {
            Vector3 v1 = meshVertices[meshIndices[index]];
            Vector3 v2 = meshVertices[meshIndices[index+1]];
            Vector3 v3 = meshVertices[meshIndices[index+2]];

            Vector3 polygonCenter = (v1 + v2 + v3) / 3;

            nodes.Add(new Node { index = index/3, center = polygonCenter, verticesIndex = index, neighboursIndex = 0 });
        };

        // Find node neighbors
        for (int index = 0; index < meshIndices.Length; index += 3)
        {
            Vector3 v11 = meshVertices[meshIndices[index]];
            Vector3 v12 = meshVertices[meshIndices[index + 1]];
            Vector3 v13 = meshVertices[meshIndices[index + 2]];

            NativeList<Vector3> verts1 = new NativeList<Vector3>(Allocator.Temp) { v11, v12, v13 };

            for (int idx = 0; idx < meshIndices.Length; idx += 3)
            {
                if (idx == index) continue;

                Vector3 v21 = meshVertices[meshIndices[idx]];
                Vector3 v22 = meshVertices[meshIndices[idx + 1]];
                Vector3 v23 = meshVertices[meshIndices[idx + 2]];

                NativeList<Vector3> verts2 = new NativeList<Vector3>(Allocator.Temp) { v21, v22, v23 };

                (bool isNeighbor, Portal portal) = EdgeInCommon(verts1, verts2);
                if (isNeighbor)
                {
                    neighbours.Add(new Neighbour { portal = portal, nodeIndex = idx / 3 });

                    Node node = nodes[index / 3];
                    node.neighboursSize += 1;
                    node.neighboursIndex = neighbours.Length - node.neighboursSize;
                    nodes[index / 3] = node;

                    //nodes[idx / 3].neighbours.Add(new Neighbour { portal = portal, nodeIndex = index / 3 });
                }

                verts2.Dispose();
            };

            verts1.Dispose();
        };

        Debug.Log("Number of nodes: " + nodes.Length);
        Debug.Log("Number of neighbours: " + neighbours.Length);

        //Debug.Log(nodes[556].neighbours.Count);

        //PathFinding(24, 541);
    }

    /// <summary>
    /// Returns a bool indicating wheter the two provided nodes have and edge in common.
    /// Also, if true, gives the edge as a portal.
    /// </summary>
    /// <param name="verts1"> Vertices of one node.</param>
    /// <param name="verts2"> Vertices of another node.</param>
    /// <param name="p"> Portal to be filled if the node are neighbours.</param>
    /// <returns></returns>
    private (bool, Portal) EdgeInCommon(NativeList<Vector3> verts1, NativeList<Vector3> verts2)
    {
        bool vertInCommon = false;
        bool edgeInCommon = false;
        Portal p = new Portal { v1 = Vector3.zero, v2 = Vector3.zero };

        foreach(Vector3 v1 in verts1)
        {
            foreach (Vector3 v2 in verts2)
            {
                float distance = Mathf.Abs((v1 - v2).magnitude);

                if (distance <= 0.00005 && !vertInCommon)
                {
                    vertInCommon = true;
                    p.v1 = v1;
                }
                else if (distance <= 0.00005 && vertInCommon)
                {
                    edgeInCommon = true;
                    p.v2 = v1;
                    break;
                }
            }
        }

        return (edgeInCommon, p);
    }

    /// <summary>
    /// This job perform an A* path search.
    /// 
    /// Inputs:
    ///     startIndex: Index of the starting node (agents current node)
    ///     endIndex: Index of the objective node
    ///     nodes: List of the nav mesh nodes
    ///     neighbours: List of node neighbours
    /// </summary>
    [BurstCompile]
    private struct PathFindingJob: IJob
    {
        public Entity entity;
        public int startIndex; //Index of the start node
        public int endIndex; //Index of the objective node
        [ReadOnly] public NativeList<Node> nodes; //List of the nav mesh nodes
        [ReadOnly] public NativeList<Neighbour> neighbours; //List of node neighbours
        //[NativeDisableParallelForRestriction] public BufferFromEntity<PathBuffer> pathBuffer;
        public NativeArray<int> parents;

        public void Execute()
        {

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closeList = new NativeList<int>(Allocator.Temp);

            //Initialize costs and parents indices
            NativeList<Vector2> costs = new NativeList<Vector2>(Allocator.Temp); //x for g and y for h costs
            //NativeList<int> parents = new NativeList<int>(Allocator.Temp);

            for (int i = 0; i < nodes.Length; i++)
            {
                float distance = Mathf.Abs((nodes[i].center - nodes[endIndex].center).magnitude);
                costs.Add(new Vector2(float.MaxValue, distance));
                //parents.Add(-1);
                parents[i] = -1;
            }

            openList.Add(startIndex);
            Vector2 startCost = costs[startIndex];
            startCost.x = 0.0f;
            costs[startIndex] = startCost;

            while (openList.Length > 0)
            {
                // Find lowest cost node
                int currentNodeIdx = GetLowestFCost(costs, openList);

                if (currentNodeIdx == endIndex) break; // Path finished

                // Remove current node from list and add it to close list
                for (int i = 0; i < openList.Length; i++)
                {
                    if (currentNodeIdx == openList[i])
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closeList.Add(currentNodeIdx);

                // Explore cuurent node neighborhood
                for (int idx = nodes[currentNodeIdx].neighboursIndex; idx < nodes[currentNodeIdx].neighboursIndex + nodes[currentNodeIdx].neighboursSize; idx++)
                {
                    int neighbourNodeIndex = neighbours[idx].nodeIndex;

                    if (closeList.Contains(neighbourNodeIndex)) continue; // Node already visited

                    float newGCost = costs[currentNodeIdx].x + (nodes[currentNodeIdx].center - nodes[neighbourNodeIndex].center).magnitude;
                    if (newGCost < costs[neighbourNodeIndex].x)
                    {
                        parents[neighbourNodeIndex] = currentNodeIdx;
                        Vector2 c = costs[neighbourNodeIndex];
                        c.x = newGCost;
                        costs[neighbourNodeIndex] = c;

                        if (!openList.Contains(neighbourNodeIndex))
                            openList.Add(neighbourNodeIndex);

                    }

                }

            }

            /*// Create Path
            //NativeList<int> pathNodes = new NativeList<int>(Allocator.Temp);
            if (parents[endIndex] == -1) Debug.Log("Path not found");
            else
            {
                // Path found
                pathBuffer[entity].Clear();
                int currentIdx = endIndex;
                //pathNodes.Add(currentIdx);
                while (parents[currentIdx] != -1)
                {
                    //pathNodes.Add(parents[currentIdx]);
                    currentIdx = parents[currentIdx];
                    pathBuffer[entity].Add( new PathBuffer{v1 = Vector3.zero, v2 = Vector3.zero, nodeIndex = currentIdx});
                }

            }
            */
            // Dispose of all native arrays
            openList.Dispose();
            closeList.Dispose();
            costs.Dispose();
            //parents.Dispose();
            //pathNodes.Dispose();
        }

        /// <summary>
        /// Return the index (int) of the node with the lowest F cost in the openList.
        /// </summary>
        /// <param name="costs"> Native array containing the cost of all nodes</param>
        /// <param name="openList"> Native list of the open list of nodes</param>
        /// <returns></returns>
        private int GetLowestFCost(NativeList<Vector2> costs, NativeList<int> openList)
        {
            int lowestCostNodeIndx = openList[0];

            for (int i = 0; i < openList.Length; i++)
            {
                if (costs[i].sqrMagnitude < costs[lowestCostNodeIndx].sqrMagnitude)
                    lowestCostNodeIndx = openList[i];
            }

            return lowestCostNodeIndx;
        }
    }

    [BurstCompile]
    private struct FindClosestNodeToAgent : IJob
    {
        public Entity agent;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<AgentData> agentComponentData;
        [ReadOnly] public NativeList<Node> nodes;
       //[ReadOnly] public NativeList<Vector3> nodeVertices;

        public void Execute()
        {
            int closestNodeIndex = nodes[0].index;
            float closestDistance = float.MaxValue;

            AgentData ad = agentComponentData[agent];
            Vector3 agentPosition = ad.position;

            for(int n = 0; n < nodes.Length; n++)
            {
                Node node = nodes[n];
                //Vector3 a = nodeVertices[node.verticesIndex];
                //Vector3 b = nodeVertices[node.verticesIndex+1];
                //Vector3 c = nodeVertices[node.verticesIndex+2];
                float distance = (node.center - agentPosition).sqrMagnitude;
                if ( distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNodeIndex = node.index;
                }
            }

            ad.navMeshNodeIndex = closestNodeIndex;
            agentComponentData[agent] = ad;
        }
    }

    [BurstCompile]
    private struct AddToPathBuffer : IJob
    {
        public Entity entity;
        public int endIndex;
        public BufferFromEntity<PathBuffer> pathBuffer;
        [ReadOnly] public NativeList<Neighbour> neighbors;
        [ReadOnly] public NativeList<Node> nodes;
        [NativeDisableContainerSafetyRestriction] public ComponentDataFromEntity<AgentData> agentComponentData;
        [DeallocateOnJobCompletion] 
        public NativeArray<int> parents;
        
        public void Execute()
        {
            // Create Path
            //NativeList<int> pathNodes = new NativeList<int>(Allocator.Temp);
            if (parents[endIndex] == -1)
            {
                Debug.Log("Path not found");
                AgentData ad = agentComponentData[entity];
                ad.pathIndex = 0;
                ad.hasPath = false;
                agentComponentData[entity] = ad;
            }
            else
            {
                // Path found
                pathBuffer[entity].Clear();
                int currentIdx = endIndex;
                //pathNodes.Add(currentIdx);
                while (parents[currentIdx] != -1)
                {
                    //pathNodes.Add(parents[currentIdx]);


                    Vector3 v1 = Vector3.zero;
                    Vector3 v2 = Vector3.zero;

                    for (int i = nodes[currentIdx].neighboursIndex; i < nodes[currentIdx].neighboursIndex + nodes[currentIdx].neighboursSize; i++)
                    {
                        if (neighbors[i].nodeIndex == parents[currentIdx])
                        {
                            v1 = neighbors[i].portal.v1;
                            v2 = neighbors[i].portal.v2;
                        }
                    }

                    pathBuffer[entity].Add(new PathBuffer { v1 = v1, v2 = v2, nodeIndex = currentIdx });
                    currentIdx = parents[currentIdx];
                }

                AgentData ad = agentComponentData[entity];
                ad.pathIndex = pathBuffer[entity].Length - 1;
                ad.destination = ad.getProyectedWayPoint(pathBuffer[entity][ad.pathIndex].v1, pathBuffer[entity][ad.pathIndex].v2);
                ad.hasPath = true;
                agentComponentData[entity] = ad;
            }
        }
    }

    protected override void OnUpdate()
    {
        NativeList<Node> nodeList = nodes;
        NativeList<Neighbour> neighbourList = neighbours;

        NativeList<JobHandle> pathFindDeps = new NativeList<JobHandle>(Allocator.Temp);
        List<PathFindingJob> pathFindJobs = new List<PathFindingJob>();

        Entities.ForEach((Entity entity, ref AgentData agentData) =>
        {
            if(agentData.navMeshNodeIndex == -1)
            {
                FindClosestNodeToAgent closestNode = new FindClosestNodeToAgent
                {
                    agent = entity,
                    agentComponentData = GetComponentDataFromEntity<AgentData>(),
                    nodes = nodeList
                };

                pathFindDeps.Add(closestNode.Schedule());
            }

            if (!agentData.hasPath && agentData.navMeshNodeIndex != -1)
            {
                PathFindingJob PathFindJob = new PathFindingJob
                {
                    entity = entity,
                    startIndex = agentData.navMeshNodeIndex,
                    endIndex = UnityEngine.Random.Range(0, nodeList.Length - 1),
                    nodes = nodeList,
                    neighbours = neighbourList,
                    parents = new NativeArray<int>(nodeList.Length, Allocator.TempJob)
                    //pathBuffer = GetBufferFromEntity<PathBuffer>()
                };
                
                pathFindDeps.Add(PathFindJob.Schedule());
                pathFindJobs.Add(PathFindJob);
            }
        });

        JobHandle.CompleteAll(pathFindDeps);

        foreach(PathFindingJob pfjob in pathFindJobs)
        {
            new AddToPathBuffer
            {
                endIndex = pfjob.endIndex,
                entity = pfjob.entity,
                nodes = nodeList,
                neighbors = neighbourList,
                pathBuffer = GetBufferFromEntity<PathBuffer>(),
                agentComponentData = GetComponentDataFromEntity<AgentData>(),
                parents = pfjob.parents
            }.Run();
        }

        pathFindDeps.Dispose();
    }

    protected override void OnDestroy()
    {
        nodes.Dispose();
        neighbours.Dispose();
        meshVertices.Dispose();
        meshIndices.Dispose();
    }
}
