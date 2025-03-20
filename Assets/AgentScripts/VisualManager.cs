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

    
    public VisualManager(AgentBehavior agentBehavior, Renderer renderer, PathfindingManager pathfindingManager, TransportManager transportManager)
    {
        agent = agentBehavior;
        agentRenderer = renderer;
        pathfinding = pathfindingManager;  // Store the passed reference
        this.transportManager = transportManager; // Store the reference
        
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
    
    private void UpdateColor(Color color)
    {
        // Update material color
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
    
    public void DrawDebugVisuals()
    {
        // Draw current mode indicator
        DrawModeIndicator();
        
        // Draw checkpoint path if available
        DrawCheckpointPath();
    }
    
    private void DrawModeIndicator()
    {
        // Get current mode from agent
        TransportMode currentMode = transportManager.CurrentMode;
        
        // Draw ray above agent showing mode
        Color indicatorColor = currentMode == TransportMode.Walking ? walkingColor : 
                              currentMode == TransportMode.Driving ? drivingColor : transitColor;
        
        Debug.DrawRay(agent.transform.position, Vector3.up * 2f, indicatorColor);
    }
    
    private void DrawCheckpointPath()
    {
        // Get path data from agent
        List<Vector3> checkpoints = pathfinding.GetCheckpoints();
        int currentIndex = pathfinding.GetCurrentCheckpointIndex();
        
        if (checkpoints == null || checkpoints.Count < 2)
            return;
            
        // Draw lines between checkpoints
        for (int i = 0; i < checkpoints.Count - 1; i++)
        {
            // Color based on checkpoint status
            Color lineColor;
            
            if (i < currentIndex)
            {
                // Passed checkpoints
                lineColor = Color.green;
            }
            else if (i == currentIndex)
            {
                // Current segment
                lineColor = Color.white;
            }
            else
            {
                // Future checkpoints
                lineColor = Color.yellow;
            }
            
            // Draw line
            Debug.DrawLine(checkpoints[i], checkpoints[i+1], lineColor);
            
            // Draw checkpoint marker
            if (i == currentIndex)
            {
                // Current checkpoint (larger)
                DebugDrawSphere(checkpoints[i], 0.5f, Color.white);
            }
            else 
            {
                // Other checkpoints
                bool onRoad = IsPositionOnRoad(checkpoints[i]);
                DebugDrawSphere(checkpoints[i], 0.3f, onRoad ? Color.red : Color.blue);
            }
        }
        
        // Draw final checkpoint
        DebugDrawSphere(checkpoints[checkpoints.Count - 1], 0.3f, Color.green);
    }
    
    private void DebugDrawSphere(Vector3 center, float radius, Color color)
    {
        // Simple wireframe sphere approximation
        int segments = 8;
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
    
    private bool IsPositionOnRoad(Vector3 position)
    {
        // Check if position is on road
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            int roadAreaMask = 1 << 3; // Assuming road is area 3
            return (hit.mask & roadAreaMask) != 0;
        }
        return false;
    }
}
