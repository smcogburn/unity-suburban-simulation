using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Manages the entire transportation network graph
/// </summary>
public class TransportNetwork : MonoBehaviour
{
    // Core data structures
    private Dictionary<string, TransportNode> nodes = new Dictionary<string, TransportNode>();
    private Dictionary<string, TransportEdge> edges = new Dictionary<string, TransportEdge>();
    
    // Lookup helpers
    private Dictionary<Vector3Int, List<TransportNode>> spatialIndex = new Dictionary<Vector3Int, List<TransportNode>>();
    private float cellSize = 5f; // Size of each spatial index cell
    
    [Header("Visualization")]
    public bool showNodes = true;
    public bool showEdges = true;
    public bool showSpatialGrid = false;
    
    // Singleton pattern
    public static TransportNetwork Instance { get; private set; }
    
    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #region Network Management
    
    /// <summary>
    /// Clear the entire network
    /// </summary>
    public void Clear()
    {
        nodes.Clear();
        edges.Clear();
        spatialIndex.Clear();
        Debug.Log("Transport network cleared");
    }
    
    /// <summary>
    /// Add a node to the network
    /// </summary>
    public TransportNode AddNode(Vector3 position, NodeType type)
    {
        string id = System.Guid.NewGuid().ToString();
        return AddNode(id, position, type);
    }
    
    /// <summary>
    /// Add a node with specific ID
    /// </summary>
    public TransportNode AddNode(string id, Vector3 position, NodeType type)
    {
        // Check if ID already exists
        if (nodes.ContainsKey(id))
        {
            Debug.LogWarning($"Node with ID {id} already exists. Using existing node.");
            return nodes[id];
        }
        
        // Create new node
        TransportNode node = new TransportNode(id, position, type);
        nodes[id] = node;
        
        // Add to spatial index
        AddToSpatialIndex(node);
        
        return node;
    }
    
    /// <summary>
    /// Add an edge between two nodes
    /// </summary>
    public TransportEdge AddEdge(TransportNode start, TransportNode end, EdgeType type)
    {
        string id = System.Guid.NewGuid().ToString();
        return AddEdge(id, start, end, type);
    }
    
    /// <summary>
    /// Add an edge with specific ID
    /// </summary>
    public TransportEdge AddEdge(string id, TransportNode start, TransportNode end, EdgeType type)
    {
        // Check if ID already exists
        if (edges.ContainsKey(id))
        {
            Debug.LogWarning($"Edge with ID {id} already exists. Using existing edge.");
            return edges[id];
        }
        
        // Check if both nodes exist
        if (!nodes.ContainsValue(start) || !nodes.ContainsValue(end))
        {
            Debug.LogError("Cannot create edge: one or both nodes are not in the network");
            return null;
        }
        
        // Create new edge
        TransportEdge edge = new TransportEdge(id, start, end, type);
        edges[id] = edge;
        
        return edge;
    }
    
    /// <summary>
    /// Get a node by ID
    /// </summary>
    public TransportNode GetNode(string id)
    {
        if (nodes.TryGetValue(id, out TransportNode node))
        {
            return node;
        }
        return null;
    }
    
    /// <summary>
    /// Get an edge by ID
    /// </summary>
    public TransportEdge GetEdge(string id)
    {
        if (edges.TryGetValue(id, out TransportEdge edge))
        {
            return edge;
        }
        return null;
    }
    
    /// <summary>
    /// Remove a node from the network
    /// </summary>
    public void RemoveNode(string id)
    {
        if (nodes.TryGetValue(id, out TransportNode node))
        {
            // Remove node from spatial index
            RemoveFromSpatialIndex(node);
            
            // Remove all connected edges
            List<TransportEdge> connectedEdges = new List<TransportEdge>(node.Connections);
            foreach (var edge in connectedEdges)
            {
                RemoveEdge(edge.ID);
            }
            
            // Remove node
            nodes.Remove(id);
        }
    }
    
    /// <summary>
    /// Remove an edge from the network
    /// </summary>
    public void RemoveEdge(string id)
    {
        if (edges.TryGetValue(id, out TransportEdge edge))
        {
            // Remove connections from nodes
            edge.StartNode.RemoveConnection(edge);
            edge.EndNode.RemoveConnection(edge);
            
            // Remove edge
            edges.Remove(id);
        }
    }
    
    #endregion
    
    #region Spatial Index
    
    /// <summary>
    /// Add a node to the spatial index
    /// </summary>
    private void AddToSpatialIndex(TransportNode node)
    {
        Vector3Int cell = WorldToGrid(node.Position);
        
        if (!spatialIndex.ContainsKey(cell))
        {
            spatialIndex[cell] = new List<TransportNode>();
        }
        
        spatialIndex[cell].Add(node);
    }
    
    /// <summary>
    /// Remove a node from the spatial index
    /// </summary>
    private void RemoveFromSpatialIndex(TransportNode node)
    {
        Vector3Int cell = WorldToGrid(node.Position);
        
        if (spatialIndex.ContainsKey(cell))
        {
            spatialIndex[cell].Remove(node);
            
            if (spatialIndex[cell].Count == 0)
            {
                spatialIndex.Remove(cell);
            }
        }
    }
    
    /// <summary>
    /// Convert world position to grid cell
    /// </summary>
    private Vector3Int WorldToGrid(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }
    
    /// <summary>
    /// Get neighboring cells around a cell
    /// </summary>
    private List<Vector3Int> GetNeighborCells(Vector3Int cell)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        
        // Add center cell
        neighbors.Add(cell);
        
        // Add the 6 adjacent cells
        neighbors.Add(new Vector3Int(cell.x + 1, cell.y, cell.z));
        neighbors.Add(new Vector3Int(cell.x - 1, cell.y, cell.z));
        neighbors.Add(new Vector3Int(cell.x, cell.y + 1, cell.z));
        neighbors.Add(new Vector3Int(cell.x, cell.y - 1, cell.z));
        neighbors.Add(new Vector3Int(cell.x, cell.y, cell.z + 1));
        neighbors.Add(new Vector3Int(cell.x, cell.y, cell.z - 1));
        
        return neighbors;
    }
    
    #endregion
    
    #region Pathfinding
    
    /// <summary>
    /// Find the closest node to a position
    /// </summary>
    public TransportNode FindClosestNode(Vector3 position, TransportModeType mode = TransportModeType.Walking, float maxDistance = 20f)
    {
        // Get grid cell for this position
        Vector3Int cell = WorldToGrid(position);
        
        // Get neighboring cells
        List<Vector3Int> neighborCells = GetNeighborCells(cell);
        
        // Find closest node
        TransportNode closestNode = null;
        float closestDistance = maxDistance;
        
        foreach (var neighborCell in neighborCells)
        {
            if (!spatialIndex.ContainsKey(neighborCell))
                continue;
                
            foreach (var node in spatialIndex[neighborCell])
            {
                // Skip if node doesn't allow this mode
                // if (!node.AllowsMode(mode))
                //     continue;
                    
                float distance = Vector3.Distance(position, node.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }
        }
        
        // If we didn't find a node in neighboring cells, search more broadly
        if (closestNode == null)
        {
            // This is inefficient but would happen rarely
            foreach (var node in nodes.Values)
            {
                if (!node.AllowsMode(mode))
                    continue;
                    
                float distance = Vector3.Distance(position, node.Position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                }
            }
        }
        
        return closestNode;
    }
    
    /// <summary>
    /// Find a path between two nodes using A* algorithm
    /// </summary>
    public List<TransportNode> FindPath(TransportNode start, TransportNode end, TransportModeType preferredMode)
    {
        if (start == null || end == null)
        {
            Debug.LogWarning("Cannot find path with null nodes");
            return new List<TransportNode>();
        }
        
        if (start == end)
        {
            return new List<TransportNode> { start };
        }
        
        // Reset pathfinding data
        foreach (var node in nodes.Values)
        {
            node.ResetPathfinding();
        }
        
        // A* implementation
        List<TransportNode> openSet = new List<TransportNode> { start };
        HashSet<TransportNode> closedSet = new HashSet<TransportNode>();
        
        start.GCost = 0;
        start.HCost = Vector3.Distance(start.Position, end.Position);
        
        while (openSet.Count > 0)
        {
            // Get node with lowest F cost
            TransportNode current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < current.FCost || 
                    (openSet[i].FCost == current.FCost && openSet[i].HCost < current.HCost))
                {
                    current = openSet[i];
                }
            }
            
            // Remove from open set, add to closed set
            openSet.Remove(current);
            closedSet.Add(current);
            
            // If reached goal, reconstruct path
            if (current == end)
            {
                return ReconstructPath(start, end);
            }
            
            // Check each connected node
            foreach (var connection in current.Connections)
            {
                // Get the node at the other end of this edge
                TransportNode neighbor = (connection.StartNode == current) ? connection.EndNode : connection.StartNode;
                
                // Skip if in closed set
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }
                
                // Skip if connection doesn't allow our preferred mode
                if (!connection.AllowsMode(preferredMode))
                {
                    continue;
                }
                
                // Calculate tentative G cost
                float edgeCost = connection.GetTravelTime(preferredMode);
                float tentativeGCost = current.GCost + edgeCost;
                
                // If not in open set, add it
                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                // If this path is worse, skip
                else if (tentativeGCost >= neighbor.GCost)
                {
                    continue;
                }
                
                // This is the best path so far
                neighbor.Parent = current;
                neighbor.GCost = tentativeGCost;
                neighbor.HCost = Vector3.Distance(neighbor.Position, end.Position);
            }
        }
        
        // No path found
        Debug.LogWarning("No path found between nodes");
        return new List<TransportNode>();
    }
    
    /// <summary>
    /// Find a path between two world positions
    /// </summary>
    public List<TransportNode> FindPath(Vector3 startPos, Vector3 endPos, TransportModeType preferredMode)
    {
        // Find closest nodes
        TransportNode startNode = FindClosestNode(startPos, preferredMode);
        TransportNode endNode = FindClosestNode(endPos, preferredMode);
        
        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("Could not find nodes near positions");
            return new List<TransportNode>();
        }
        
        return FindPath(startNode, endNode, preferredMode);
    }
    
    /// <summary>
    /// Reconstruct path from A* result
    /// </summary>
    private List<TransportNode> ReconstructPath(TransportNode start, TransportNode end)
    {
        List<TransportNode> path = new List<TransportNode>();
        TransportNode current = end;
        
        // Work backwards from end to start
        while (current != null && current != start)
        {
            path.Add(current);
            current = current.Parent;
        }
        
        // Add start node
        if (current == start)
        {
            path.Add(start);
        }
        
        // Reverse to get start-to-end order
        path.Reverse();
        
        return path;
    }
    
    #endregion
    
    #region Visualization
    
    /// <summary>
    /// Draw debug visualization of the network
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw nodes
        if (showNodes)
        {
            foreach (var node in nodes.Values)
            {
                Gizmos.color = node.DebugColor;
                Gizmos.DrawSphere(node.Position, node.DebugSize);
            }
        }
        
        // Draw edges
        if (showEdges)
        {
            foreach (var edge in edges.Values)
            {
                Gizmos.color = edge.DebugColor;
                
                if (edge.ControlPoints.Count == 0)
                {
                    // Straight line
                    Gizmos.DrawLine(edge.StartNode.Position, edge.EndNode.Position);
                }
                else
                {
                    // Draw segments with control points
                    Vector3 lastPoint = edge.StartNode.Position;
                    
                    foreach (var point in edge.ControlPoints)
                    {
                        Gizmos.DrawLine(lastPoint, point);
                        lastPoint = point;
                    }
                    
                    Gizmos.DrawLine(lastPoint, edge.EndNode.Position);
                }
            }
        }
        
        // Draw spatial grid
        if (showSpatialGrid)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            
            foreach (var cell in spatialIndex.Keys)
            {
                Vector3 cellCenter = new Vector3(
                    cell.x * cellSize + cellSize/2,
                    cell.y * cellSize + cellSize/2,
                    cell.z * cellSize + cellSize/2
                );
                
                Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize);
            }
        }
    }
    
    #endregion
    
    #region Statistics and Analysis
    
    /// <summary>
    /// Log network statistics
    /// </summary>
    public void LogNetworkStats()
    {
        Debug.Log($"Transport Network Stats: {nodes.Count} nodes, {edges.Count} edges");
        
        // Count by type
        Dictionary<NodeType, int> nodeTypeCounts = new Dictionary<NodeType, int>();
        Dictionary<EdgeType, int> edgeTypeCounts = new Dictionary<EdgeType, int>();
        
        foreach (var node in nodes.Values)
        {
            if (!nodeTypeCounts.ContainsKey(node.Type))
            {
                nodeTypeCounts[node.Type] = 0;
            }
            nodeTypeCounts[node.Type]++;
        }
        
        foreach (var edge in edges.Values)
        {
            if (!edgeTypeCounts.ContainsKey(edge.Type))
            {
                edgeTypeCounts[edge.Type] = 0;
            }
            edgeTypeCounts[edge.Type]++;
        }
        
        // Log node types
        string nodeTypeStr = "Node types: ";
        foreach (var kvp in nodeTypeCounts)
        {
            nodeTypeStr += $"{kvp.Key}: {kvp.Value}, ";
        }
        Debug.Log(nodeTypeStr);
        
        // Log edge types
        string edgeTypeStr = "Edge types: ";
        foreach (var kvp in edgeTypeCounts)
        {
            edgeTypeStr += $"{kvp.Key}: {kvp.Value}, ";
        }
        Debug.Log(edgeTypeStr);
    }
    
    #endregion
}