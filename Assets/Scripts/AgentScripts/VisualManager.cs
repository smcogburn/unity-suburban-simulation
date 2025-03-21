// VisualManager.cs - Enhanced visual representation and debugging
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Manages visual representation and debugging
public class VisualManager
{
    private AgentBehavior agent;
    private Renderer agentRenderer;
    private Material agentMaterial;
    
    // Color settings
    private Color walkingColor = Color.blue;
    private Color drivingColor = Color.red;
    private Color transitColor = Color.green;
    
    private PathfindingManager pathfinding;
    private TransportManager transportManager;
    
    // Debug visualization settings
    private float waypointSphereSize = 0.3f;
    private float currentWaypointSphereSize = 0.5f;
    private bool showPathToNextWaypoint = true;
    
    // Constructor
    public VisualManager(AgentBehavior agentBehavior, Renderer renderer, PathfindingManager pathfindingManager, TransportManager transportManager)
    {
        agent = agentBehavior;
        agentRenderer = renderer;
        pathfinding = pathfindingManager;
        this.transportManager = transportManager;
        
        // Create material for the agent
        if (agentRenderer != null)
        {
            // Create instance of material to avoid affecting other objects
            agentMaterial = new Material(agentRenderer.material);
            agentRenderer.material = agentMaterial;
        }
        else
        {
            Debug.LogWarning("No renderer found on agent or its children!");
        }
    }
    
    // Update visuals based on transport mode
    public void UpdateVisuals(TransportMode mode)
    {
        // Update color based on transport mode
        Color targetColor = walkingColor;
        
        switch (mode)
        {
            case TransportMode.Walking:
                targetColor = walkingColor;
                break;
                
            case TransportMode.Driving:
                targetColor = drivingColor;
                break;
                
            case TransportMode.Transit:
                targetColor = transitColor;
                break;
        }
        
        // Apply color
        UpdateColor(targetColor);
    }
    
    // Update the material color
    private void UpdateColor(Color color)
    {
        if (agentMaterial != null)
        {
            agentMaterial.color = color;
        }
        else if (agentRenderer != null)
        {
            // Try to recreate material if needed
            agentMaterial = new Material(agentRenderer.material);
            agentRenderer.material = agentMaterial;
            agentMaterial.color = color;
        }
    }
    
    // Draw debug visualizations
    public void DrawDebugVisuals()
    {
        // Draw current mode indicator
        DrawModeIndicator();
        
        // Draw waypoint path
        DrawWaypointPath();
        
        // Draw current NavMesh path if enabled
        if (showPathToNextWaypoint)
        {
            DrawCurrentNavMeshPath();
        }
    }
    
    // Draw indicator showing current transport mode
    private void DrawModeIndicator()
    {
        TransportMode currentMode = transportManager.CurrentMode;
        
        Color indicatorColor = currentMode == TransportMode.Walking ? walkingColor : 
                              currentMode == TransportMode.Driving ? drivingColor : transitColor;
        
        Debug.DrawRay(agent.transform.position, Vector3.up * 2f, indicatorColor);
    }
    
    // Draw the waypoint path
    private void DrawWaypointPath()
    {
        List<Vector3> waypoints = pathfinding.GetWaypoints();
        int currentIndex = pathfinding.GetCurrentWaypointIndex();
        
        if (waypoints == null || waypoints.Count < 2)
            return;
        
        // Draw lines between waypoints
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            // Color based on waypoint status
            Color lineColor;
            
            if (i < currentIndex)
            {
                // Passed waypoints
                lineColor = Color.green;
            }
            else if (i == currentIndex)
            {
                // Current segment
                lineColor = Color.white;
            }
            else
            {
                // Future waypoints
                lineColor = Color.yellow;
            }
            
            // Draw line
            Debug.DrawLine(waypoints[i], waypoints[i+1], lineColor);
            
            // Draw waypoint marker
            if (i == currentIndex)
            {
                // Current waypoint (larger)
                DebugDrawSphere(waypoints[i], currentWaypointSphereSize, Color.white);
            }
            else 
            {
                // Other waypoints
                bool onRoad = IsPositionOnRoad(waypoints[i]);
                Color waypointColor = onRoad ? Color.red : Color.blue;
                DebugDrawSphere(waypoints[i], waypointSphereSize, waypointColor);
            }
        }
        
        // Draw final waypoint
        bool finalOnRoad = IsPositionOnRoad(waypoints[waypoints.Count - 1]);
        Color finalColor = finalOnRoad ? Color.red : Color.green;
        DebugDrawSphere(waypoints[waypoints.Count - 1], waypointSphereSize, finalColor);
    }
    
    // Draw the current NavMesh path
    private void DrawCurrentNavMeshPath()
    {
        NavMeshAgent navAgent = agent.GetComponent<NavMeshAgent>();
        
        if (navAgent == null || navAgent.path == null)
            return;
            
        // Path visualization
        Color pathColor = transportManager.CurrentMode == TransportMode.Walking ? 
                         new Color(0.5f, 0.5f, 1f, 0.5f) : 
                         new Color(1f, 0.3f, 0.3f, 0.5f);
            
        // Draw the current path as calculated by NavMesh
        for (int i = 0; i < navAgent.path.corners.Length - 1; i++)
        {
            Debug.DrawLine(navAgent.path.corners[i], navAgent.path.corners[i + 1], pathColor);
        }
    }
    
    // Draw a debug sphere
    private void DebugDrawSphere(Vector3 center, float radius, Color color)
    {
        int segments = 12;
        float step = 360.0f / segments;
        
        // Draw three orthogonal circles
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * step * Mathf.Deg2Rad;
            float angle2 = (i + 1) * step * Mathf.Deg2Rad;
            
            Vector3 pos1, pos2;
            
            // XY plane
            pos1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            pos2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
            Debug.DrawLine(pos1, pos2, color);
            
            // XZ plane
            pos1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            pos2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(pos1, pos2, color);
            
            // YZ plane
            pos1 = center + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
            pos2 = center + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
            Debug.DrawLine(pos1, pos2, color);
        }
    }
    
    // Check if a position is on a road
    private bool IsPositionOnRoad(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            int roadAreaMask = 1 << 3; // Assuming road is area 3
            return (hit.mask & roadAreaMask) != 0;
        }
        return false;
    }
}