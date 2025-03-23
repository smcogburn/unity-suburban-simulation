using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Types of transportation edges
/// </summary>
public enum EdgeType
{
    Road,           // Car road
    Sidewalk,       // Pedestrian path
    BusRoute,       // Bus line
    TrainRoute,     // Train/subway line
    BikeLane,       // Dedicated bike lane
    Crosswalk       // Pedestrian crossing
}

/// <summary>
/// Represents a connection between nodes in the transport network
/// </summary>
public class TransportEdge
{
    // Core properties
    public string ID { get; private set; }
    public TransportNode StartNode { get; set; }
    public TransportNode EndNode { get; set; }
    public EdgeType Type { get; set; }
    public float Length { get; private set; }
    
    // Direction properties
    public bool IsBidirectional { get; set; } = true;
    
    // Speed and capacity
    public float BaseSpeed { get; set; } = 10f;        // Base speed in m/s (about 36 km/h)
    public int Capacity { get; set; } = 10;            // How many agents can use this edge at once
    
    // Current state
    public int CurrentOccupancy { get; set; } = 0;
    public float CongestionLevel => (float)CurrentOccupancy / Capacity;
    public float CurrentSpeed => Mathf.Lerp(BaseSpeed, BaseSpeed * 0.2f, CongestionLevel);
    
    // Allowed transport modes
    public HashSet<TransportModeType> AllowedModes { get; private set; } = new HashSet<TransportModeType>();
    
    // For visualization
    public List<Vector3> ControlPoints { get; set; } = new List<Vector3>();
    public Color DebugColor { get; set; } = Color.white;
    
    /// <summary>
    /// Constructor for a transport edge
    /// </summary>
    public TransportEdge(string id, TransportNode start, TransportNode end, EdgeType type)
    {
        ID = id;
        StartNode = start;
        EndNode = end;
        Type = type;
        
        // Calculate length based on node positions
        UpdateLength();
        
        // Set default properties based on edge type
        SetDefaultProperties();
        
        // Connect this edge to its nodes
        start.AddConnection(this);
        end.AddConnection(this);
    }
    
    /// <summary>
    /// Set default properties based on edge type
    /// </summary>
    private void SetDefaultProperties()
    {
        switch (Type)
        {
            case EdgeType.Road:
                BaseSpeed = 10f;      // 36 km/h
                Capacity = 10;
                AllowedModes.Add(TransportModeType.Driving);
                DebugColor = Color.gray;
                break;
                
            case EdgeType.Sidewalk:
                BaseSpeed = 1.4f;     // 5 km/h
                Capacity = 20;
                AllowedModes.Add(TransportModeType.Walking);
                DebugColor = Color.white;
                break;
                
            case EdgeType.BusRoute:
                BaseSpeed = 8f;       // 29 km/h
                Capacity = 5;
                AllowedModes.Add(TransportModeType.Transit);
                DebugColor = Color.green;
                break;
                
            case EdgeType.TrainRoute:
                BaseSpeed = 15f;      // 54 km/h
                Capacity = 3;
                AllowedModes.Add(TransportModeType.Transit);
                DebugColor = Color.blue;
                break;
                
            case EdgeType.BikeLane:
                BaseSpeed = 4f;       // 14 km/h
                Capacity = 15;
                AllowedModes.Add(TransportModeType.Biking);
                DebugColor = Color.cyan;
                break;
                
            case EdgeType.Crosswalk:
                BaseSpeed = 1.2f;     // 4.3 km/h (slightly slower than normal walking)
                Capacity = 15;
                AllowedModes.Add(TransportModeType.Walking);
                DebugColor = Color.yellow;
                break;
        }
    }
    
    /// <summary>
    /// Update the length of this edge based on node positions
    /// </summary>
    public void UpdateLength()
    {
        if (ControlPoints.Count > 0)
        {
            // If we have control points, calculate length along the path
            Length = 0;
            Vector3 lastPoint = StartNode.Position;
            
            foreach (var point in ControlPoints)
            {
                Length += Vector3.Distance(lastPoint, point);
                lastPoint = point;
            }
            
            Length += Vector3.Distance(lastPoint, EndNode.Position);
        }
        else
        {
            // Direct line between nodes
            Length = Vector3.Distance(StartNode.Position, EndNode.Position);
        }
    }
    
    /// <summary>
    /// Add a control point to this edge (for curved paths)
    /// </summary>
    public void AddControlPoint(Vector3 point, int index = -1)
    {
        if (index < 0 || index >= ControlPoints.Count)
        {
            ControlPoints.Add(point);
        }
        else
        {
            ControlPoints.Insert(index, point);
        }
        
        UpdateLength();
    }
    
    /// <summary>
    /// Get a point along this edge based on normalized position (0-1)
    /// </summary>
    public Vector3 GetPointAlongEdge(float t)
    {
        t = Mathf.Clamp01(t);
        
        if (ControlPoints.Count == 0)
        {
            // Direct line between nodes
            return Vector3.Lerp(StartNode.Position, EndNode.Position, t);
        }
        
        // With control points, we need to determine which segment t falls in
        List<Vector3> allPoints = new List<Vector3> { StartNode.Position };
        allPoints.AddRange(ControlPoints);
        allPoints.Add(EndNode.Position);
        
        // Calculate segment lengths and total length
        float totalLength = 0;
        List<float> segmentLengths = new List<float>();
        
        for (int i = 0; i < allPoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(allPoints[i], allPoints[i + 1]);
            segmentLengths.Add(segmentLength);
            totalLength += segmentLength;
        }
        
        // Find which segment t falls in
        float targetDistance = t * totalLength;
        float distanceSoFar = 0;
        
        for (int i = 0; i < segmentLengths.Count; i++)
        {
            if (targetDistance <= distanceSoFar + segmentLengths[i])
            {
                // t is in this segment
                float segmentT = (targetDistance - distanceSoFar) / segmentLengths[i];
                return Vector3.Lerp(allPoints[i], allPoints[i + 1], segmentT);
            }
            
            distanceSoFar += segmentLengths[i];
        }
        
        // Failsafe
        return EndNode.Position;
    }
    
    /// <summary>
    /// Check if a transport mode is allowed on this edge
    /// </summary>
    public bool AllowsMode(TransportModeType mode)
    {
        return AllowedModes.Contains(mode);
    }
    
    /// <summary>
    /// Add an allowed transport mode
    /// </summary>
    public void AddAllowedMode(TransportModeType mode)
    {
        AllowedModes.Add(mode);
    }
    
    /// <summary>
    /// Register an agent using this edge
    /// </summary>
    public void RegisterAgent()
    {
        CurrentOccupancy++;
    }
    
    /// <summary>
    /// Unregister an agent from this edge
    /// </summary>
    public void UnregisterAgent()
    {
        if (CurrentOccupancy > 0)
        {
            CurrentOccupancy--;
        }
    }
    
    /// <summary>
    /// Get the travel time for this edge
    /// </summary>
    public float GetTravelTime(TransportModeType mode)
    {
        // If mode not allowed, return infinity
        if (!AllowsMode(mode))
        {
            return float.MaxValue;
        }
        
        // Calculate time based on length and current speed
        return Length / CurrentSpeed;
    }
}