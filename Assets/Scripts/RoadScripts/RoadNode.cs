// RoadNode.cs - Node representation for road network
using UnityEngine;
using System.Collections.Generic;

public class RoadNode : MonoBehaviour
{
    [Header("Node Properties")]
    public RoadSegment parentRoad;  // The road segment this node belongs to
    public List<RoadNode> connections = new List<RoadNode>(); // Nodes this node connects to
    public float distanceAlongRoad; // Distance along parent road (0-1)
    
    [Header("Node Type")]
    public bool isIntersection = false;
    public bool isEndpoint = false;
    public bool isEntryPoint = false; // Can be used to enter/exit the road
    
    [Header("Debug")]
    public Color nodeColor = Color.blue;
    public float nodeSize = 0.5f;
    
    // Cache for pathfinding
    //[System.NonSerialized]
    public float gCost; // Cost from start node
    // [System.NonSerialized]
    public float hCost; // Heuristic cost to end
    // [System.NonSerialized]
    public float fCost { get { return gCost + hCost; } }
    // [System.NonSerialized]
    public RoadNode parent; // For path reconstruction
    
    public void Initialize(RoadSegment road, float distanceParam, bool isEnd = false, bool isInter = false)
    {
        parentRoad = road;
        distanceAlongRoad = Mathf.Clamp01(distanceParam);
        isEndpoint = isEnd;
        isIntersection = isInter;
        
        // Entry points are endpoints or intersections by default
        isEntryPoint = isEndpoint || isIntersection;
        
        // Set color based on node type
        if (isIntersection)
            nodeColor = Color.yellow;
        else if (isEndpoint)
            nodeColor = Color.red;
        else
            nodeColor = Color.blue;
    }
    
    // Add a connection to another node
    public void AddConnection(RoadNode node)
    {
        if (!connections.Contains(node))
        {
            connections.Add(node);
        }
    }
    
    // Get world position
    public Vector3 GetPosition()
    {
        if (parentRoad == null)
            return transform.position;
            
        return parentRoad.GetPointAlongRoad(distanceAlongRoad);
    }
    
    // Get cost to travel to another node
    public float GetCostTo(RoadNode other)
    {
        // Basic distance
        float distance = Vector3.Distance(GetPosition(), other.GetPosition());
        
        // If both nodes are on the same road, factor in congestion
        if (parentRoad != null && parentRoad == other.parentRoad)
        {
            float congestionFactor = 1f + parentRoad.congestionLevel;
            return distance * congestionFactor;
        }
        
        return distance;
    }
    
    void OnDrawGizmos()
    {
        // Draw node
        Gizmos.color = nodeColor;
        Gizmos.DrawSphere(GetPosition(), nodeSize);
        
        // Draw connections
        Gizmos.color = Color.gray;
        foreach (var connection in connections)
        {
            if (connection != null)
            {
                Gizmos.DrawLine(GetPosition(), connection.GetPosition());
            }
        }
    }
}