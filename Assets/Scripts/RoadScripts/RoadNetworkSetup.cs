// RoadNetworkSetup.cs - Enhanced with node-based pathfinding
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class RoadNetworkSetup : MonoBehaviour
{
    [Header("Road Network Setup")]
    [Tooltip("Layer mask to identify objects that should be marked as roads")]
    public LayerMask roadLayerMask;
    
    [Tooltip("The NavMesh area index for roads")]
    public int roadAreaIndex = 3;
    
    [Header("Road Graph Settings")]
    public bool regenerateNetworkOnStart = true;
    public bool connectDisjointRoads = true;
    [Tooltip("Maximum distance to connect disjoint roads")]
    public float maxDisjointConnectionDistance = 10f;
    
    [Header("Road Network Debug")]
    public bool showRoadConnections = true;
    public bool showRoadNodes = true;
    public bool showRoadPathfinding = false;
    
    private RoadSegment[] roadSegments;
    private List<RoadNode> allRoadNodes = new List<RoadNode>();
    
    // Singleton pattern for easy access
    public static RoadNetworkSetup Instance { get; private set; }
    
    void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (regenerateNetworkOnStart)
        {
            GenerateRoadNetwork();
        }
    }
    
    // Generate or regenerate the complete road network
    public void GenerateRoadNetwork()
    {
        Debug.Log("Generating road network...");
        
        // Find all road segments in the scene
        roadSegments = FindObjectsOfType<RoadSegment>();
        
        if (roadSegments.Length == 0)
        {
            Debug.LogWarning("No RoadSegment components found in the scene. Road network will not function properly.");
            return;
        }
        
        Debug.Log($"Found {roadSegments.Length} road segments in the scene.");
        
        // Generate nodes for each road segment
        foreach (var road in roadSegments)
        {
            if (road != null)
            {
                road.GenerateRoadNodes();
            }
        }
        
        // Find connections between road segments
        UpdateRoadConnections();
        
        // Connect disjoint road segments if enabled
        if (connectDisjointRoads)
        {
            ConnectDisjointRoads();
        }
        
        // Collect all road nodes for pathfinding
        CollectAllRoadNodes();
        
        Debug.Log("Road network generation complete.");
    }
    
    // Update connections between road segments
    public void UpdateRoadConnections()
    {
        foreach (var road in roadSegments)
        {
            if (road != null)
            {
                road.FindConnections(roadSegments);
            }
        }
        
        Debug.Log($"Updated connections for {roadSegments.Length} road segments.");
    }
    
    // Collect all road nodes for pathfinding
    private void CollectAllRoadNodes()
    {
        allRoadNodes.Clear();
        
        foreach (var road in roadSegments)
        {
            if (road != null)
            {
                allRoadNodes.AddRange(road.GetRoadNodes());
            }
        }
        
        Debug.Log($"Collected {allRoadNodes.Count} road nodes for pathfinding.");
    }
    
    // Connect disjoint road segments that are close but not touching
    private void ConnectDisjointRoads()
    {
        int connectionsAdded = 0;
        
        // Find all road nodes
        List<RoadNode> endpointNodes = new List<RoadNode>();
        foreach (var road in roadSegments)
        {
            if (road != null)
            {
                foreach (var node in road.GetRoadNodes())
                {
                    if (node.isEndpoint)
                    {
                        endpointNodes.Add(node);
                    }
                }
            }
        }
        
        // Check each endpoint against others
        for (int i = 0; i < endpointNodes.Count; i++)
        {
            for (int j = i + 1; j < endpointNodes.Count; j++)
            {
                RoadNode nodeA = endpointNodes[i];
                RoadNode nodeB = endpointNodes[j];
                
                // Skip if they're on the same road
                if (nodeA.parentRoad == nodeB.parentRoad)
                    continue;
                    
                // Check if these road segments are already connected
                if (nodeA.parentRoad.connectedRoads.Contains(nodeB.parentRoad))
                    continue;
                    
                // Check distance
                float distance = Vector3.Distance(nodeA.GetPosition(), nodeB.GetPosition());
                if (distance <= maxDisjointConnectionDistance)
                {
                    // Connect the nodes
                    nodeA.AddConnection(nodeB);
                    nodeB.AddConnection(nodeA);
                    
                    // Mark as intersections
                    nodeA.isIntersection = true;
                    nodeB.isIntersection = true;
                    
                    // Add road connection
                    nodeA.parentRoad.connectedRoads.Add(nodeB.parentRoad);
                    nodeB.parentRoad.connectedRoads.Add(nodeA.parentRoad);
                    
                    connectionsAdded++;
                }
            }
        }
        
        Debug.Log($"Added {connectionsAdded} connections between disjoint roads.");
    }
    
    // Find nearest road segment to a position
    public RoadSegment FindNearestRoadSegment(Vector3 position, float maxDistance = 20f)
    {
        if (roadSegments == null || roadSegments.Length == 0)
        {
            roadSegments = FindObjectsOfType<RoadSegment>();
            if (roadSegments.Length == 0)
                return null;
        }
        
        RoadSegment nearest = null;
        float minDistance = maxDistance;
        
        foreach (var road in roadSegments)
        {
            if (road == null) continue;
            
            // Calculate closest point on road segment
            Vector3 closestPoint = road.GetClosestPointOnRoad(position);
            float distance = Vector3.Distance(position, closestPoint);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = road;
            }
        }
        
        return nearest;
    }
    
    // Find nearest road node to a position
    public RoadNode FindNearestRoadNode(Vector3 position, float maxDistance = 20f, bool entryPointsOnly = false)
    {
        if (allRoadNodes.Count == 0)
        {
            CollectAllRoadNodes();
            if (allRoadNodes.Count == 0)
                return null;
        }
        
        RoadNode nearest = null;
        float minDistance = maxDistance;
        
        foreach (var node in allRoadNodes)
        {
            if (node == null) continue;
            
            // Skip if we only want entry points and this isn't one
            if (entryPointsOnly && !node.isEntryPoint)
                continue;
                
            // Calculate distance
            float distance = Vector3.Distance(position, node.GetPosition());
            
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = node;
            }
        }
        
        return nearest;
    }
    
    // Find nearest entry point to a position
    public RoadNode FindNearestEntryPoint(Vector3 position, float maxDistance = 20f)
    {
        return FindNearestRoadNode(position, maxDistance, true);
    }
    
    // Find path through road network using A* algorithm with road nodes
    public List<RoadNode> FindPathThroughRoadNetwork(RoadNode startNode, RoadNode endNode)
    {
        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("Cannot find path with null start or end node");
            return new List<RoadNode>();
        }
        
        if (startNode == endNode)
        {
            return new List<RoadNode> { startNode };
        }
        
        // Implementation of A* algorithm for road network
        var openSet = new List<RoadNode> { startNode };
        var closedSet = new HashSet<RoadNode>();
        
        // Initialize all nodes for pathfinding
        foreach (var node in allRoadNodes)
        {
            node.gCost = float.MaxValue;
            node.parent = null;
        }
        
        // Set initial values for start node
        startNode.gCost = 0;
        startNode.hCost = GetNodeDistance(startNode, endNode);
        
        while (openSet.Count > 0)
        {
            // Find node with lowest fCost
            RoadNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || 
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }
            
            // Remove current from open set and add to closed set
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            // If reached goal, reconstruct and return path
            if (currentNode == endNode)
            {
                return ReconstructPath(startNode, endNode);
            }
            
            // Check each connected node
            foreach (var neighbor in currentNode.connections)
            {
                if (neighbor == null || closedSet.Contains(neighbor))
                    continue;
                
                // Calculate tentative gCost to this neighbor
                float tentativeGCost = currentNode.gCost + currentNode.GetCostTo(neighbor);
                
                // If not in open set, add it
                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                // If this path is worse, skip
                else if (tentativeGCost >= neighbor.gCost)
                {
                    continue;
                }
                
                // This is the best path so far
                neighbor.parent = currentNode;
                neighbor.gCost = tentativeGCost;
                neighbor.hCost = GetNodeDistance(neighbor, endNode);
            }
        }
        
        // No path found
        Debug.LogWarning("No path found between road nodes");
        return new List<RoadNode>();
    }
    
    // Helper to reconstruct path from A* result
    private List<RoadNode> ReconstructPath(RoadNode startNode, RoadNode endNode)
    {
        var path = new List<RoadNode>();
        RoadNode current = endNode;
        
        // Work backwards from end to start
        while (current != null && current != startNode)
        {
            path.Add(current);
            current = current.parent;
        }
        
        // Add start node and reverse
        if (current == startNode)
        {
            path.Add(startNode);
        }
        
        path.Reverse();
        return path;
    }
    
    // Calculate distance between nodes
    private float GetNodeDistance(RoadNode a, RoadNode b)
    {
        return Vector3.Distance(a.GetPosition(), b.GetPosition());
    }
    
    // Find path through road network using legacy road segments (for compatibility)
    public List<RoadSegment> FindPathThroughRoadNetwork(RoadSegment start, RoadSegment end)
    {
        if (start == null || end == null)
        {
            return new List<RoadSegment>();
        }
        
        if (start == end)
        {
            return new List<RoadSegment> { start };
        }
        
        // Find path using nodes
        RoadNode startNode = start.GetEndNode();
        RoadNode endNode = end.GetStartNode();
        
        var nodePath = FindPathThroughRoadNetwork(startNode, endNode);
        
        // Convert node path back to segment path
        var segmentPath = new List<RoadSegment>();
        RoadSegment currentSegment = null;
        
        foreach (var node in nodePath)
        {
            if (currentSegment != node.parentRoad)
            {
                if (node.parentRoad != null && !segmentPath.Contains(node.parentRoad))
                {
                    segmentPath.Add(node.parentRoad);
                }
                currentSegment = node.parentRoad;
            }
        }
        
        return segmentPath;
    }
    
    // Get all road segments
    public RoadSegment[] GetRoadSegments()
    {
        if (roadSegments == null || roadSegments.Length == 0)
        {
            roadSegments = FindObjectsOfType<RoadSegment>();
        }
        
        return roadSegments;
    }
    
    // Get all road nodes
    public List<RoadNode> GetAllRoadNodes()
    {
        if (allRoadNodes.Count == 0)
        {
            CollectAllRoadNodes();
        }
        
        return allRoadNodes;
    }
    
    void OnDrawGizmos()
    {
        // Draw road connections
        if (showRoadConnections)
        {
            // Find all road segments in the scene
            RoadSegment[] roads = FindObjectsOfType<RoadSegment>();
            
            // Draw connections between road segments
            foreach (var road in roads)
            {
                if (road == null) continue;
                
                // Draw road center
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(road.transform.position, 0.5f);
                
                // Draw connections to other roads
                Gizmos.color = Color.yellow;
                foreach (var connection in road.connectedRoads)
                {
                    if (connection != null)
                    {
                        Gizmos.DrawLine(road.transform.position, connection.transform.position);
                    }
                }
            }
        }
        
        // Draw road nodes
        if (showRoadNodes)
        {
            // Find all road nodes in the scene
            RoadNode[] nodes = FindObjectsOfType<RoadNode>();
            
            foreach (var node in nodes)
            {
                if (node == null) continue;
                
                // Node color depends on type
                Gizmos.color = node.nodeColor;
                
                // Draw node
                Gizmos.DrawSphere(node.GetPosition(), node.nodeSize);
                
                // Draw connections
                Gizmos.color = Color.gray;
                foreach (var connection in node.connections)
                {
                    if (connection != null)
                    {
                        Gizmos.DrawLine(node.GetPosition(), connection.GetPosition());
                    }
                }
            }
        }
    }
    
    // Utility method to check if a position is on a road
    public static bool IsPositionOnRoad(Vector3 position, int roadAreaIndex)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            int roadMask = 1 << roadAreaIndex;
            return (hit.mask & roadMask) != 0;
        }
        return false;
    }
    
    // Utility method to find the nearest road point to a position
    public static Vector3 FindNearestRoadPoint(Vector3 position, int roadAreaIndex, float maxDistance = 10f)
    {
        NavMeshHit hit;
        int roadMask = 1 << roadAreaIndex;
        
        if (NavMesh.SamplePosition(position, out hit, maxDistance, roadMask))
        {
            return hit.position;
        }
        
        return position; // Return original position if no road found
    }
}