using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Transportation node types
/// </summary>
public enum NodeType
{
    Intersection,     // Junction of multiple roads
    RoadPoint,        // Point along a road
    EntryPoint,       // Point where agents can enter/exit a road
    TransitStop,      // Bus/train stop
    TransferPoint     // Point where agent can change transport modes
}

/// <summary>
/// Represents a node in the transportation network graph
/// </summary>
public class TransportNode
{
    // Core properties
    public string ID { get; private set; }
    public Vector3 Position { get; set; }
    public NodeType Type { get; set; }
    
    // Connection information
    public List<TransportEdge> Connections { get; private set; } = new List<TransportEdge>();
    public HashSet<TransportModeType> AllowedModes { get; private set; } = new HashSet<TransportModeType>();
    
    // Visual representation
    public GameObject VisualObject { get; set; }
    
    // For debugging
    public Color DebugColor { get; set; } = Color.blue;
    public float DebugSize { get; set; } = 0.5f;
    
    // For pathfinding
    public float GCost { get; set; } = float.MaxValue;  // Cost from start
    public float HCost { get; set; } = 0;               // Estimated cost to goal
    public TransportNode Parent { get; set; } = null;   // For path reconstruction
    public float FCost => GCost + HCost;               // Total cost

    /// <summary>
    /// Constructor for creating a new transport node
    /// </summary>
    public TransportNode(string id, Vector3 position, NodeType type)
    {
        ID = id;
        Position = position;
        Type = type;
        
        // Set default debug appearance based on type
        switch (type)
        {
            case NodeType.Intersection:
                DebugColor = Color.yellow;
                DebugSize = 0.7f;
                break;
            case NodeType.EntryPoint:
                DebugColor = Color.green;
                DebugSize = 0.6f;
                break;
            case NodeType.TransitStop:
                DebugColor = Color.cyan;
                DebugSize = 0.8f;
                break;
            case NodeType.TransferPoint:
                DebugColor = Color.magenta;
                DebugSize = 0.8f;
                break;
        }
    }
    
    /// <summary>
    /// Add a connection to another node via an edge
    /// </summary>
    public void AddConnection(TransportEdge edge)
    {
        if (!Connections.Contains(edge))
        {
            Connections.Add(edge);
        }
    }
    
    /// <summary>
    /// Remove a connection to another node
    /// </summary>
    public void RemoveConnection(TransportEdge edge)
    {
        Connections.Remove(edge);
    }
    
    /// <summary>
    /// Add a transport mode this node allows
    /// </summary>
    public void AddAllowedMode(TransportModeType mode)
    {
        AllowedModes.Add(mode);
    }
    
    /// <summary>
    /// Check if this node allows a specific transport mode
    /// </summary>
    public bool AllowsMode(TransportModeType mode)
    {
        return AllowedModes.Contains(mode);
    }
    
    /// <summary>
    /// Get all connected nodes
    /// </summary>
    public List<TransportNode> GetConnectedNodes()
    {
        List<TransportNode> nodes = new List<TransportNode>();
        
        foreach (var edge in Connections)
        {
            // Get the node at the other end of this edge
            TransportNode otherNode = (edge.StartNode == this) ? edge.EndNode : edge.StartNode;
            nodes.Add(otherNode);
        }
        
        return nodes;
    }
    
    /// <summary>
    /// Reset pathfinding data for this node
    /// </summary>
    public void ResetPathfinding()
    {
        GCost = float.MaxValue;
        HCost = 0;
        Parent = null;
    }
    
    /// <summary>
    /// Get the edge connecting this node to another node
    /// </summary>
    public TransportEdge GetEdgeTo(TransportNode other)
    {
        foreach (var edge in Connections)
        {
            if ((edge.StartNode == this && edge.EndNode == other) ||
                (edge.EndNode == this && edge.StartNode == other))
            {
                return edge;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if this node is connected to another
    /// </summary>
    public bool IsConnectedTo(TransportNode other)
    {
        return GetEdgeTo(other) != null;
    }
}