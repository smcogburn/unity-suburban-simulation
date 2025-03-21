// PathfindingManager.cs - Manages navigation and waypoints based on journey plan
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Manages pathfinding, waypoints, and navigation
public class PathfindingManager
{
    private AgentBehavior agent;
    private NavMeshAgent navAgent;
    private JourneyPlanner journeyPlanner;
    
    // Path and waypoint data
    private List<Vector3> waypoints = new List<Vector3>();
    private List<TransportMode> waypointModes = new List<TransportMode>();
    private int currentWaypointIndex = 0;
    private Vector3 finalDestination;
    private bool reachedFinalDestination = true;
    
    // Settings
    public float waypointReachedDistance = 1.5f;
    
    // Events
    public delegate void WaypointEvent(Vector3 position, bool isFinal, TransportMode mode);
    public event WaypointEvent OnWaypointReached;
    
    // Constructor
    public PathfindingManager(AgentBehavior agentBehavior, NavMeshAgent navMeshAgent)
    {
        agent = agentBehavior;
        navAgent = navMeshAgent;
        
        // Create journey planner with default settings
        JourneyPlanner.JourneySettings settings = new JourneyPlanner.JourneySettings();
        journeyPlanner = new JourneyPlanner(RoadNetworkSetup.Instance, settings);
        
        // Update settings based on the specific needs of this simulation
        settings.longJourneyThreshold = 10f;  // When to consider driving vs walking
        settings.roadEntryThreshold = 5f;     // Maximum distance to look for nearby roads
        settings.maxRoadDetourFactor = 1.5f;  // How much longer a road route can be vs direct path
    }
    
    // Set a new destination for the agent
    public void SetDestination(Vector3 targetPosition)
    {
        Debug.Log($"Agent setting destination to {targetPosition}");
        
        reachedFinalDestination = false;
        finalDestination = targetPosition;
        
        // Plan the journey
        Journey journey = journeyPlanner.PlanJourney(agent.transform.position, targetPosition);
        
        // Get waypoints and transport modes from journey
        waypoints = journey.GetAllWaypoints();
        waypointModes = journey.GetTransportModes();
        
        // Reset waypoint index
        currentWaypointIndex = 0;
        
        // Start moving to first waypoint
        if (waypoints.Count > 0)
        {
            ProcessNextWaypoint();
        }
        else
        {
            Debug.LogWarning("No waypoints generated for journey");
            reachedFinalDestination = true;
        }
    }
    
    // Process the next waypoint in the journey
    private void ProcessNextWaypoint()
    {
        // Safety check
        if (currentWaypointIndex >= waypoints.Count)
        {
            reachedFinalDestination = true;
            return;
        }
        
        Vector3 targetWaypoint = waypoints[currentWaypointIndex];
        bool isFinalWaypoint = (currentWaypointIndex == waypoints.Count - 1);
        
        // Get transport mode for this segment
        TransportMode transportMode = TransportMode.Walking; // Default
        if (currentWaypointIndex < waypointModes.Count)
        {
            transportMode = waypointModes[currentWaypointIndex];
        }
        
        // Set appropriate stopping distance based on waypoint type
        navAgent.stoppingDistance = isFinalWaypoint ? 0.5f : 0.1f;
        
        // Notify listeners about the new waypoint and transport mode
        OnWaypointReached?.Invoke(targetWaypoint, isFinalWaypoint, transportMode);
        
        // Set navigation target
        navAgent.SetDestination(targetWaypoint);
        
        Debug.Log($"Moving to waypoint {currentWaypointIndex + 1}/{waypoints.Count} using {transportMode} mode");
    }
    
    // Update method called every frame
    public void Update()
    {
        // Skip if we're already at destination or waiting for path
        if (reachedFinalDestination || navAgent.pathPending)
            return;
        
        // Check if we've reached the current waypoint
        if (Vector3.Distance(agent.transform.position, waypoints[currentWaypointIndex]) <= waypointReachedDistance)
        {
            // Check if this was the final waypoint
            if (currentWaypointIndex >= waypoints.Count - 1)
            {
                reachedFinalDestination = true;
                Debug.Log("Reached final destination");
                
                // Notify listeners we've reached the final destination
                OnWaypointReached?.Invoke(finalDestination, true, TransportMode.Walking);
            }
            else
            {
                // Move to next waypoint
                currentWaypointIndex++;
                ProcessNextWaypoint();
            }
        }
    }
    
    // Get all waypoints
    public List<Vector3> GetWaypoints()
    {
        return waypoints;
    }
    
    // Get current waypoint index
    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }
    
    // Check if destination has been reached
    public bool HasReachedDestination()
    {
        return reachedFinalDestination;
    }
    
    // Get transport mode for current segment
    public TransportMode GetCurrentTransportMode()
    {
        if (waypointModes != null && currentWaypointIndex < waypointModes.Count)
        {
            return waypointModes[currentWaypointIndex];
        }
        return TransportMode.Walking;
    }
}