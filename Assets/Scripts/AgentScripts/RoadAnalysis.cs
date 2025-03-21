// RoadAnalysis.cs - Simplified version for road detection and analysis
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// Helper class for road analysis and detection
public class RoadAnalysis
{
    private NavMeshAgent agent;
    
    // Settings
    private int roadAreaIndex = 3;
    private float roadEdgeDetectionStep = 0.5f;
    private float maxRoadWidth = 10f;
    
    public RoadAnalysis(NavMeshAgent navAgent)
    {
        agent = navAgent;
    }
    
    // Check if a position is on a road
    public bool IsPositionOnRoad(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            int roadAreaMask = 1 << roadAreaIndex;
            return (hit.mask & roadAreaMask) != 0;
        }
        return false;
    }
    
    // Find the nearest valid position on a road
    public Vector3 FindNearestRoadPosition(Vector3 position, float maxDistance = 10f)
    {
        NavMeshHit hit;
        int roadAreaMask = 1 << roadAreaIndex;
        
        if (NavMesh.SamplePosition(position, out hit, maxDistance, roadAreaMask))
        {
            return hit.position;
        }
        
        return Vector3.zero; // No valid position found
    }
    
    // Get a road center point (to avoid edges)
    public Vector3 GetRoadCenterPoint(Vector3 position)
    {
        if (!IsPositionOnRoad(position))
            return position;
            
        // Cast rays in multiple directions to find road edges
        List<float> edgeDistances = new List<float>();
        
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            
            float distToEdge = FindDistanceToRoadEdge(position, direction);
            
            if (distToEdge > 0.1f && distToEdge < maxRoadWidth)
            {
                edgeDistances.Add(distToEdge);
            }
        }
        
        // No edges found, return original position
        if (edgeDistances.Count < 4)
            return position;
            
        // Find opposite pairs for better center estimation
        Vector3 avgDirection = Vector3.zero;
        
        for (int i = 0; i < 4; i++)
        {
            float angle1 = i * 45f * Mathf.Deg2Rad;
            float angle2 = (i + 4) * 45f * Mathf.Deg2Rad;
            
            Vector3 dir1 = new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1));
            Vector3 dir2 = new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2));
            
            float dist1 = FindDistanceToRoadEdge(position, dir1);
            float dist2 = FindDistanceToRoadEdge(position, dir2);
            
            if (dist1 > 0.1f && dist2 > 0.1f)
            {
                avgDirection += dir1 * (dist1 - dist2);
            }
        }
        
        // Move toward the center based on the average direction
        Vector3 centerPoint = position - avgDirection.normalized * (avgDirection.magnitude / 2f);
        
        // Ensure center is on road
        NavMeshHit hit;
        if (NavMesh.SamplePosition(centerPoint, out hit, 1.0f, 1 << roadAreaIndex))
        {
            return hit.position;
        }
        
        return position;
    }
    
    // Find the distance to the edge of a road in a given direction
    private float FindDistanceToRoadEdge(Vector3 startPoint, Vector3 direction)
    {
        if (!IsPositionOnRoad(startPoint))
            return 0f;
            
        float currentDistance = roadEdgeDetectionStep;
        
        while (currentDistance <= maxRoadWidth)
        {
            Vector3 testPoint = startPoint + direction * currentDistance;
            
            if (!IsPositionOnRoad(testPoint))
            {
                return currentDistance - (roadEdgeDetectionStep / 2f);
            }
            
            currentDistance += roadEdgeDetectionStep;
        }
        
        return maxRoadWidth;
    }
    
    // Check if a position is at a road intersection
    public bool IsIntersection(Vector3 position, float radius = 3f)
    {
        if (!IsPositionOnRoad(position))
            return false;
            
        int connectionCount = 0;
        int directions = 8;
        
        for (int i = 0; i < directions; i++)
        {
            float angle = i * (360f / directions) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            
            // Check at the edge of the radius
            Vector3 checkPoint = position + direction * radius;
            
            if (IsPositionOnRoad(checkPoint))
            {
                connectionCount++;
            }
        }
        
        // Consider it an intersection if roads lead in 3+ directions
        return connectionCount >= 3;
    }
    
    // Find the entry point to a road when coming from off-road
    public Vector3 FindRoadEntryPoint(Vector3 startOffRoad, Vector3 endOnRoad)
    {
        Vector3 direction = (endOnRoad - startOffRoad).normalized;
        float distance = Vector3.Distance(startOffRoad, endOnRoad);
        float step = 0.5f;
        
        Vector3 lastOffRoadPoint = startOffRoad;
        
        for (float t = step; t <= distance; t += step)
        {
            Vector3 testPoint = startOffRoad + direction * t;
            bool onRoad = IsPositionOnRoad(testPoint);
            
            if (onRoad)
            {
                // Found the entry point
                return testPoint;
            }
            
            lastOffRoadPoint = testPoint;
        }
        
        // Couldn't find entry point, return the end point
        return endOnRoad;
    }
    
    // Find the exit point from a road when going to off-road
    public Vector3 FindRoadExitPoint(Vector3 startOnRoad, Vector3 endOffRoad)
    {
        Vector3 direction = (endOffRoad - startOnRoad).normalized;
        float distance = Vector3.Distance(startOnRoad, endOffRoad);
        float step = 0.5f;
        
        Vector3 lastOnRoadPoint = startOnRoad;
        
        for (float t = step; t <= distance; t += step)
        {
            Vector3 testPoint = startOnRoad + direction * t;
            bool onRoad = IsPositionOnRoad(testPoint);
            
            if (!onRoad)
            {
                // Found the exit point - return the last on-road point
                return lastOnRoadPoint;
            }
            
            if (onRoad)
            {
                lastOnRoadPoint = testPoint;
            }
        }
        
        // Couldn't find exit point, return the start point
        return startOnRoad;
    }
}