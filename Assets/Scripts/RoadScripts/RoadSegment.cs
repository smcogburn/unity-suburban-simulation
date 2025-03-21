// RoadSegment.cs - Enhanced version with node support
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RoadSegment : MonoBehaviour
{
    [Header("Road Properties")]
    public float length;
    public float width = 3f;
    public int capacity = 10;
    public float baseSpeed = 10f;
    
    [Header("Node Configuration")]
    [Tooltip("Number of intermediate nodes to place along this road")]
    public int intermediateNodeCount = 3;
    [Tooltip("Should this road automatically add entry points at regular intervals?")]
    public bool addRegularEntryPoints = true;
    [Tooltip("Distance between entry points (in meters)")]
    public float entryPointSpacing = 5f;
    
    [Header("Current State")]
    public int currentOccupancy = 0;
    public float currentSpeed;
    [Range(0f, 1f)]
    public float congestionLevel = 0f;
    
    [Header("Road Connections")]
    [Tooltip("Other road segments that connect to this one")]
    public List<RoadSegment> connectedRoads = new List<RoadSegment>();
    
    [Tooltip("Maximum distance to search for connected roads")]
    public float connectionDistance = 3f;
    
    // Nodes along this road
    private List<RoadNode> roadNodes = new List<RoadNode>();
    
    // Start and end nodes (cached for quick access)
    private RoadNode startNode;
    private RoadNode endNode;
    
    private List<GameObject> vehiclesOnRoad = new List<GameObject>();
    
    void Awake()
    {
        // Calculate length based on transform if not manually set
        if (length <= 0)
        {
            length = transform.localScale.z;
        }
        
        currentSpeed = baseSpeed;
    }
    
    void Start()
    {
        // Calculate length based on transform if not manually set
        if (length <= 0)
        {
            Vector3 scale = transform.localScale;
            length = Mathf.Max(scale.x, scale.z);
            Debug.Log($"Road {name} - Calculated length: {length}");
        }
        
        currentSpeed = baseSpeed;
        
        // Draw debug lines showing the road's direction
        Vector3 direction = RoadDirectionUtility.GetRoadDirection(transform);
        Debug.DrawRay(transform.position, direction * 10f, Color.magenta, 5.0f);
        Debug.Log($"Road {name} - Direction: {direction}");
        
        // Generate nodes for this road segment
        GenerateRoadNodes();
    }
    
    public void GenerateRoadNodes()
    {
        // Clear any existing nodes
        foreach (var node in roadNodes)
        {
            if (node != null)
                Destroy(node.gameObject);
        }
        roadNodes.Clear();
        
        // Calculate how many nodes we need in total
        int totalNodes = intermediateNodeCount + 2; // +2 for start and end
        
        // Create parent object for nodes if it doesn't exist
        Transform nodesParent = transform.Find("Nodes");
        if (nodesParent == null)
        {
            GameObject nodeParentObj = new GameObject("Nodes");
            nodeParentObj.transform.parent = transform;
            nodeParentObj.transform.localPosition = Vector3.zero;
            nodeParentObj.transform.localRotation = Quaternion.identity; // Make sure rotation is reset
            nodesParent = nodeParentObj.transform;
        }
        
        // Log road's basic information for debugging
        Debug.Log($"Road {name} - Scale: {transform.localScale}, Length: {length}, " +
                 $"Start: {GetStartPoint()}, End: {GetEndPoint()}");
        
        // Create start and end nodes
        GameObject startNodeObj = new GameObject("StartNode");
        startNodeObj.transform.parent = nodesParent;
        startNode = startNodeObj.AddComponent<RoadNode>();
        startNode.Initialize(this, 0f, true, false);
        roadNodes.Add(startNode);
        
        GameObject endNodeObj = new GameObject("EndNode");
        endNodeObj.transform.parent = nodesParent;
        endNode = endNodeObj.AddComponent<RoadNode>();
        endNode.Initialize(this, 1f, true, false);
        roadNodes.Add(endNode);
        
        // Create intermediate nodes
        for (int i = 1; i <= intermediateNodeCount; i++)
        {
            float t = (float)i / (intermediateNodeCount + 1);
            GameObject nodeObj = new GameObject($"Node_{i}");
            nodeObj.transform.parent = nodesParent;
            RoadNode node = nodeObj.AddComponent<RoadNode>();
            node.Initialize(this, t, false, false);
            roadNodes.Add(node);
        }
        
        // Add entry points if enabled
        if (addRegularEntryPoints && length > entryPointSpacing)
        {
            int entryPointCount = Mathf.FloorToInt(length / entryPointSpacing) - 1;
            for (int i = 1; i <= entryPointCount; i++)
            {
                float t = (float)i / (entryPointCount + 1);
                
                // Check if there's already a node close to this position
                bool nodeExists = false;
                foreach (var node in roadNodes)
                {
                    if (Mathf.Abs(node.distanceAlongRoad - t) < 0.05f)
                    {
                        nodeExists = true;
                        node.isEntryPoint = true;
                        break;
                    }
                }
                
                if (!nodeExists)
                {
                    GameObject nodeObj = new GameObject($"EntryPoint_{i}");
                    nodeObj.transform.parent = nodesParent;
                    RoadNode node = nodeObj.AddComponent<RoadNode>();
                    node.Initialize(this, t, false, false);
                    node.isEntryPoint = true;
                    roadNodes.Add(node);
                }
            }
        }
        
        // Connect nodes along this road
        ConnectRoadNodes();
    }
    
    private void ConnectRoadNodes()
    {
        // Sort nodes by distance along road
        roadNodes.Sort((a, b) => a.distanceAlongRoad.CompareTo(b.distanceAlongRoad));
        
        // Connect consecutive nodes
        for (int i = 0; i < roadNodes.Count - 1; i++)
        {
            roadNodes[i].AddConnection(roadNodes[i + 1]);
            roadNodes[i + 1].AddConnection(roadNodes[i]);
        }
    }
    
    public void FindConnections(RoadSegment[] allRoads)
    {
        connectedRoads.Clear();
        
        // Find connections to other roads
        foreach (var otherRoad in allRoads)
        {
            // Skip self
            if (otherRoad == this) continue;
            
            // Check if start or end nodes are close to any nodes on the other road
            foreach (var thisNode in roadNodes)
            {
                if (!thisNode.isEndpoint) continue; // Only check endpoints for connections
                
                foreach (var otherNode in otherRoad.GetRoadNodes())
                {
                    if (!otherNode.isEndpoint) continue; // Only connect to endpoints
                    
                    // If nodes are close enough, connect the roads and the nodes
                    if (Vector3.Distance(thisNode.GetPosition(), otherNode.GetPosition()) < connectionDistance)
                    {
                        // Add road connection
                        if (!connectedRoads.Contains(otherRoad))
                        {
                            connectedRoads.Add(otherRoad);
                        }
                        
                        // Add node connection
                        thisNode.AddConnection(otherNode);
                        otherNode.AddConnection(thisNode);
                        
                        // Mark as intersection
                        thisNode.isIntersection = true;
                        otherNode.isIntersection = true;
                    }
                }
            }
        }
    }
    
    // Get all nodes along this road
    public List<RoadNode> GetRoadNodes()
    {
        return roadNodes;
    }
    
    // Get start and end nodes
    public RoadNode GetStartNode() { return startNode; }
    public RoadNode GetEndNode() { return endNode; }
    
    // Find the nearest node to a given position
    public RoadNode FindNearestNode(Vector3 position, bool entryPointsOnly = false)
    {
        RoadNode nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var node in roadNodes)
        {
            // Skip if we only want entry points and this isn't one
            if (entryPointsOnly && !node.isEntryPoint)
                continue;
                
            float distance = Vector3.Distance(position, node.GetPosition());
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = node;
            }
        }
        
        return nearest;
    }
    
    // Find the nearest entry point to a given position
    public RoadNode FindNearestEntryPoint(Vector3 position)
    {
        return FindNearestNode(position, true);
    }
    
    // Get the start point of this road segment
    public Vector3 GetStartPoint()
    {
        return transform.position - transform.forward * (length / 2);
    }
    
    // Get the end point of this road segment
    public Vector3 GetEndPoint()
    {
        return transform.position + transform.forward * (length / 2);
    }
    
    // Get a point along the road segment based on a parameter t (0-1)
    public Vector3 GetPointAlongRoad(float t)
    {
        t = Mathf.Clamp01(t);
        return Vector3.Lerp(GetStartPoint(), GetEndPoint(), t);
    }
    
    // Find closest point on this road to a given position
    public Vector3 GetClosestPointOnRoad(Vector3 position)
    {
        Vector3 startPoint = GetStartPoint();
        Vector3 endPoint = GetEndPoint();
        
        Vector3 roadDirection = (endPoint - startPoint).normalized;
        float roadLength = (endPoint - startPoint).magnitude;
        
        float t = Vector3.Dot(position - startPoint, roadDirection) / roadLength;
        t = Mathf.Clamp01(t);
        
        return startPoint + roadDirection * (t * roadLength);
    }
    
    // Return how fast traffic can move on this road (0-1)
    public float GetSpeedRatio()
    {
        return currentSpeed / baseSpeed;
    }
    
    public void RegisterVehicle(GameObject vehicle)
    {
        if (!vehiclesOnRoad.Contains(vehicle))
        {
            vehiclesOnRoad.Add(vehicle);
            currentOccupancy = vehiclesOnRoad.Count;
            UpdateCurrentSpeed();
        }
    }
    
    public void UnregisterVehicle(GameObject vehicle)
    {
        if (vehiclesOnRoad.Contains(vehicle))
        {
            vehiclesOnRoad.Remove(vehicle);
            currentOccupancy = vehiclesOnRoad.Count;
            UpdateCurrentSpeed();
        }
    }
    
    void UpdateCurrentSpeed()
    {
        // Simple traffic model - speed decreases as occupancy increases
        congestionLevel = Mathf.Clamp01((float)currentOccupancy / capacity);
        currentSpeed = Mathf.Lerp(baseSpeed, baseSpeed * 0.2f, congestionLevel);
        
        // If this road is congested, agents on this road should be notified
        if (congestionLevel > 0.5f)
        {
            NotifyAgentsOfCongestion();
        }
    }
    
    void NotifyAgentsOfCongestion()
    {
        // Find agents on this road and notify them of congestion
        List<AgentBehavior> agents = FindAgentsOnRoad();
        
        foreach (var agent in agents)
        {
            if (agent != null)
            {
                agent.NotifyCongestionChanged(this);
            }
        }
    }
    
    // For finding agents on this road segment
    public List<AgentBehavior> FindAgentsOnRoad()
    {
        List<AgentBehavior> agents = new List<AgentBehavior>();
        
        // Get all agents in the scene
        AgentBehavior[] allAgents = FindObjectsOfType<AgentBehavior>();
        
        foreach (var agent in allAgents)
        {
            // Define bounds of the road (assuming roads are box-shaped)
            Bounds roadBounds = new Bounds(transform.position, transform.localScale);
            
            // Expand bounds a bit to catch agents that are on the road
            roadBounds.Expand(0.5f);
            
            // Check if agent is within the road bounds
            if (roadBounds.Contains(agent.transform.position))
            {
                agents.Add(agent);
            }
        }
        
        return agents;
    }
    
    void OnDrawGizmos()
    {
        // Visual debugging
        float congestion = Mathf.Clamp01((float)currentOccupancy / capacity);
        Gizmos.color = Color.Lerp(Color.green, Color.red, congestion);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Draw road endpoints
        Gizmos.color = Color.blue;
        Vector3 startPoint = GetStartPoint();
        Vector3 endPoint = GetEndPoint();
        Gizmos.DrawSphere(startPoint, 0.3f);
        Gizmos.DrawSphere(endPoint, 0.3f);
    }
}