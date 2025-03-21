// JourneyPlanner.cs - Enhanced for node-based pathfinding
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// This class handles the planning of journeys, including transport mode selection
public class JourneyPlanner
{
    // Settings for journey planning
    [System.Serializable]
    public class JourneySettings
    {
        public float longJourneyThreshold = 20f; // Distance in meters to consider driving
        public float roadEntryThreshold = 5f;    // Max distance to find road entry point
        public float maxRoadDetourFactor = 1.5f; // How much longer a road route can be to be considered
        public float minDrivingDistance = 10f;   // Minimum distance to consider driving (avoid short drives)
    }
    
    // Reference to road network
    private RoadNetworkSetup roadNetwork;
    
    // Settings
    private JourneySettings settings;
    
    // Constructor
    public JourneyPlanner(RoadNetworkSetup roadNetworkSetup, JourneySettings journeySettings)
    {
        roadNetwork = roadNetworkSetup;
        settings = journeySettings;
    }
    
    // Main planning method
    public Journey PlanJourney(Vector3 start, Vector3 destination)
    {
        Debug.Log($"Planning journey from {start} to {destination}");
        
        // Create a new journey
        Journey journey = new Journey();
        
        // Calculate straight line distance for reference
        float directDistance = Vector3.Distance(start, destination);
        
        // Check if journey is short enough to just walk
        if (directDistance <= settings.longJourneyThreshold)
        {
            Debug.Log($"Journey distance {directDistance}m is below threshold, planning walking route");
            // For short journeys, just walk directly
            journey.AddSegment(start, destination, TransportMode.Walking);
            return journey;
        }
        else 
        {
            Debug.Log($"Journey distance {directDistance}m is above threshold, planning multimodal route");
        }
        
        // For longer journeys, check if we can use the road network
        
        // Find nearest entry points to the road network
        RoadNode startEntryNode = roadNetwork.FindNearestEntryPoint(start, settings.roadEntryThreshold);
        RoadNode endEntryNode = roadNetwork.FindNearestEntryPoint(destination, settings.roadEntryThreshold);
        
        // If we can't find suitable entry/exit points, just walk
        if (startEntryNode == null || endEntryNode == null)
        {
            Debug.Log("Could not find suitable road entry points, planning walking route");
            journey.AddSegment(start, destination, TransportMode.Walking);
            return journey;
        }
        
        Debug.Log($"Found road entry points near start and destination");
        
        // Get positions of entry/exit points
        Vector3 startRoadPoint = startEntryNode.GetPosition();
        Vector3 endRoadPoint = endEntryNode.GetPosition();
        
        // Find path through road network
        List<RoadNode> roadPath = roadNetwork.FindPathThroughRoadNetwork(startEntryNode, endEntryNode);
        
        // If no road path found, just walk directly
        if (roadPath.Count == 0)
        {
            Debug.Log("No valid road path found, planning walking route");
            journey.AddSegment(start, destination, TransportMode.Walking);
            return journey;
        }
        
        Debug.Log($"Found road path with {roadPath.Count} nodes");
        
        // Calculate road path distance
        float roadPathDistance = CalculatePathDistance(roadPath);
        
        // Calculate walking distances
        float walkToRoadDistance = Vector3.Distance(start, startRoadPoint);
        float walkFromRoadDistance = Vector3.Distance(endRoadPoint, destination);
        float totalWalkDistance = walkToRoadDistance + walkFromRoadDistance;
        
        // Calculate driving distance
        float drivingDistance = roadPathDistance - totalWalkDistance;
        
        // If driving distance is too short, just walk the whole way
        if (drivingDistance < settings.minDrivingDistance)
        {
            Debug.Log($"Driving distance ({drivingDistance}m) is too short, planning walking route");
            journey.AddSegment(start, destination, TransportMode.Walking);
            return journey;
        }
        
        // If road route is too long compared to direct route, just walk
        if ((roadPathDistance + totalWalkDistance) > directDistance * settings.maxRoadDetourFactor)
        {
            Debug.Log($"Road path distance exceeds detour threshold, planning walking route");
            journey.AddSegment(start, destination, TransportMode.Walking);
            return journey;
        }
        
        // We have a good road path, now plan a multi-modal journey
        Debug.Log("Planning multi-modal journey");
        
        // First segment: Walk to the first road entry point
        if (walkToRoadDistance > 0.1f)
        {
            journey.AddSegment(start, startRoadPoint, TransportMode.Walking);
        }
        
        // Middle segments: Drive along road network
        List<Vector3> drivingWaypoints = new List<Vector3>();
        
        // First driving waypoint is start entry point
        drivingWaypoints.Add(startRoadPoint);
        
        // Add intermediate waypoints at road nodes (skipping start/end)
        for (int i = 1; i < roadPath.Count - 1; i++)
        {
            // Only add nodes that are intersections or endpoints (avoid too many waypoints)
            if (roadPath[i].isIntersection || roadPath[i].isEndpoint)
            {
                drivingWaypoints.Add(roadPath[i].GetPosition());
            }
        }
        
        // Last driving waypoint is end entry point
        drivingWaypoints.Add(endRoadPoint);
        
        // Add driving segments
        for (int i = 0; i < drivingWaypoints.Count - 1; i++)
        {
            journey.AddSegment(drivingWaypoints[i], drivingWaypoints[i + 1], TransportMode.Driving);
        }
        
        // Last segment: Walk from last road to destination
        if (walkFromRoadDistance > 0.1f)
        {
            journey.AddSegment(endRoadPoint, destination, TransportMode.Walking);
        }
        
        Debug.Log($"Journey planned with {journey.segments.Count} segments");
        return journey;
    }
    
    // Calculate total distance along a path of road nodes
    private float CalculatePathDistance(List<RoadNode> nodePath)
    {
        float totalDistance = 0f;
        
        if (nodePath.Count <= 1)
            return 0f;
            
        for (int i = 0; i < nodePath.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(nodePath[i].GetPosition(), nodePath[i + 1].GetPosition());
        }
        
        return totalDistance;
    }
}

// Class to represent a complete journey with multiple segments
public class Journey
{
    // A segment is a part of the journey with a single transport mode
    public class Segment
    {
        public Vector3 startPoint;
        public Vector3 endPoint;
        public TransportMode transportMode;
        
        public Segment(Vector3 start, Vector3 end, TransportMode mode)
        {
            startPoint = start;
            endPoint = end;
            transportMode = mode;
        }
    }
    
    // List of segments in this journey
    public List<Segment> segments = new List<Segment>();
    
    // Add a segment to the journey
    public void AddSegment(Vector3 start, Vector3 end, TransportMode mode)
    {
        segments.Add(new Segment(start, end, mode));
    }
    
    // Get the total number of waypoints (start points plus final end point)
    public int GetWaypointCount()
    {
        return segments.Count > 0 ? segments.Count + 1 : 0;
    }
    
    // Get all waypoints as a flat list
    public List<Vector3> GetAllWaypoints()
    {
        List<Vector3> waypoints = new List<Vector3>();
        
        // Add starting point of each segment
        foreach (var segment in segments)
        {
            waypoints.Add(segment.startPoint);
        }
        
        // Add the final endpoint if there are any segments
        if (segments.Count > 0)
        {
            waypoints.Add(segments[segments.Count - 1].endPoint);
        }
        
        return waypoints;
    }
    
    // Get all transport modes as a list (one per segment)
    public List<TransportMode> GetTransportModes()
    {
        List<TransportMode> modes = new List<TransportMode>();
        
        foreach (var segment in segments)
        {
            modes.Add(segment.transportMode);
        }
        
        return modes;
    }
    
    // Calculate total journey distance
    public float CalculateTotalDistance()
    {
        float totalDistance = 0f;
        
        foreach (var segment in segments)
        {
            totalDistance += Vector3.Distance(segment.startPoint, segment.endPoint);
        }
        
        return totalDistance;
    }
    
    // Calculate estimated journey time
    public float CalculateEstimatedTime()
    {
        float totalTime = 0f;
        
        foreach (var segment in segments)
        {
            float distance = Vector3.Distance(segment.startPoint, segment.endPoint);
            float speed = 0f;
            
            // Approximate speeds for different transport modes
            switch (segment.transportMode)
            {
                case TransportMode.Walking:
                    speed = 1.4f; // meters per second (~5 km/h)
                    break;
                case TransportMode.Driving:
                    speed = 8.3f; // meters per second (~30 km/h)
                    break;
                case TransportMode.Transit:
                    speed = 5.5f; // meters per second (~20 km/h)
                    break;
            }
            
            totalTime += distance / speed;
        }
        
        return totalTime;
    }
}