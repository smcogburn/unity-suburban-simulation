// RoadSegment.cs - attach to road objects
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RoadSegment : MonoBehaviour
{
    [Header("Road Properties")]
    public float length;
    public int capacity = 10;
    public float baseSpeed = 10f;
    
    [Header("Current State")]
    public int currentOccupancy = 0;
    public float currentSpeed;
    
    [Header("Road Connections")]
    [Tooltip("Other road segments that connect to this one")]
    public List<RoadSegment> connectedRoads = new List<RoadSegment>();
    
    [Tooltip("Maximum distance to search for connected roads")]
    public float connectionDistance = 3f;
    
    private List<GameObject> vehiclesOnRoad = new List<GameObject>();
    
    void Start()
    {
        // Calculate length based on transform
        length = transform.localScale.z;
        currentSpeed = baseSpeed;
        
        // Find connections to other road segments
        FindConnections(FindObjectsOfType<RoadSegment>());
    }
    
    public void FindConnections(RoadSegment[] allRoads)
    {
        connectedRoads.Clear();
        
        // Define the endpoints of this road segment (assuming it's aligned along local Z axis)
        Vector3 startPoint = transform.position - transform.forward * (length / 2);
        Vector3 endPoint = transform.position + transform.forward * (length / 2);
        
        foreach (var otherRoad in allRoads)
        {
            // Skip self
            if (otherRoad == this) continue;
            
            // Define the endpoints of the other road segment
            Vector3 otherStartPoint = otherRoad.transform.position - otherRoad.transform.forward * (otherRoad.length / 2);
            Vector3 otherEndPoint = otherRoad.transform.position + otherRoad.transform.forward * (otherRoad.length / 2);
            
            // Check if any endpoints are close enough to be considered connected
            if (Vector3.Distance(startPoint, otherStartPoint) < connectionDistance ||
                Vector3.Distance(startPoint, otherEndPoint) < connectionDistance ||
                Vector3.Distance(endPoint, otherStartPoint) < connectionDistance ||
                Vector3.Distance(endPoint, otherEndPoint) < connectionDistance)
            {
                connectedRoads.Add(otherRoad);
            }
        }
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
        float congestionFactor = Mathf.Clamp01((float)currentOccupancy / capacity);
        currentSpeed = Mathf.Lerp(baseSpeed, baseSpeed * 0.2f, congestionFactor);
        
        // If this road is congested, agents on this road should be notified
        if (congestionFactor > 0.5f)
        {
            // Find agents on this road and potentially update their paths
            NotifyAgentsOfCongestion();
        }
    }
    
    void NotifyAgentsOfCongestion()
    {
        // This method could be implemented to notify agents of congestion
        // So they can recalculate their paths
    }
    
    // For finding agents on NavMesh roads
    public List<AgentBehavior> FindAgentsOnRoad()
    {
        List<AgentBehavior> agents = new List<AgentBehavior>();
        
        // Get all agents in the scene
        AgentBehavior[] allAgents = FindObjectsOfType<AgentBehavior>();
        
        foreach (var agent in allAgents)
        {
            // Define bounds of the road (assuming roads are box-shaped)
            Bounds roadBounds = new Bounds(transform.position, transform.localScale);
            
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
        Vector3 startPoint = transform.position - transform.forward * (length / 2);
        Vector3 endPoint = transform.position + transform.forward * (length / 2);
        Gizmos.DrawSphere(startPoint, 0.3f);
        Gizmos.DrawSphere(endPoint, 0.3f);
    }
}