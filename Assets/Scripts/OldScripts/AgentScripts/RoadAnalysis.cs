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
    private int roadEdgeRayCount = 16;
    private float intersectionDetectionRadius = 3f;
    
    public RoadAnalysis(NavMeshAgent navAgent)
    {
        agent = navAgent;
    }
    
    public bool IsPositionOnRoad(Vector3 position)
    {
        // Check if a position is on a road
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            int roadAreaMask = 1 << roadAreaIndex;
            return (hit.mask & roadAreaMask) != 0;
        }
        return false;
    }
    
    public bool IsIntersection(Vector3 position)
    {
        // Skip check if not on road
        if (!IsPositionOnRoad(position))
            return false;
            
        int roadConnections = 0;
        float rayLength = intersectionDetectionRadius;
        List<Vector3> roadDirections = new List<Vector3>();
        
        // Check in multiple directions
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position + direction * rayLength, out hit, 1.0f, 1 << roadAreaIndex))
            {
                if (IsPositionOnRoad(hit.position))
                {
                    roadConnections++;
                    roadDirections.Add(direction);
                }
            }
        }
        
        // Analyze road directions to detect true intersections
        if (roadConnections >= 3)
            return true;
            
        // Check for perpendicular roads (T-junctions)
        if (roadDirections.Count >= 2)
        {
            for (int i = 0; i < roadDirections.Count; i++)
            {
                for (int j = i + 1; j < roadDirections.Count; j++)
                {
                    // Calculate angle between directions
                    float dot = Vector3.Dot(roadDirections[i], roadDirections[j]);
                    // If roads are approximately perpendicular
                    if (Mathf.Abs(dot) < 0.3f)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    public Vector3 GetRoadCenterPoint(Vector3 position)
    {
        // Only try to center if on a road
        if (!IsPositionOnRoad(position))
            return position;
            
        // Find edges by casting rays
        List<Vector3> edgePoints = new List<Vector3>();
        List<float> edgeDistances = new List<float>();
        
        for (int i = 0; i < roadEdgeRayCount; i++)
        {
            float angle = i * (360f / roadEdgeRayCount) * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            
            float distToEdge = FindDistanceToRoadEdge(position, direction);
            
            if (distToEdge > 0.1f && distToEdge < maxRoadWidth)
            {
                Vector3 edgePoint = position + direction * distToEdge;
                edgePoints.Add(edgePoint);
                edgeDistances.Add(distToEdge);
            }
        }
        
        // With enough edge points, find center
        if (edgePoints.Count >= 4)
        {
            // Find opposite pairs for better center estimation
            Vector3 avgCenter = Vector3.zero;
            int pairsFound = 0;
            
            for (int i = 0; i < edgePoints.Count; i++)
            {
                int oppositeIndex = (i + edgePoints.Count / 2) % edgePoints.Count;
                Vector3 midpoint = (edgePoints[i] + edgePoints[oppositeIndex]) / 2f;
                avgCenter += midpoint;
                pairsFound++;
            }
            
            if (pairsFound > 0)
            {
                avgCenter /= pairsFound;
                
                // Ensure center is on road
                NavMeshHit hit;
                if (NavMesh.SamplePosition(avgCenter, out hit, 1.0f, 1 << roadAreaIndex))
                {
                    return hit.position;
                }
            }
        }
        
        // Fallback
        return position;
    }
    
    public float FindDistanceToRoadEdge(Vector3 startPoint, Vector3 direction)
    {
        // Skip if not starting on road
        if (!IsPositionOnRoad(startPoint))
            return 0f;
            
        float currentDistance = roadEdgeDetectionStep;
        
        while (currentDistance <= maxRoadWidth)
        {
            Vector3 testPoint = startPoint + direction * currentDistance;
            
            if (!IsPositionOnRoad(testPoint))
            {
                // Found edge, return distance to just before edge
                return currentDistance - roadEdgeDetectionStep/2;
            }
            
            currentDistance += roadEdgeDetectionStep;
        }
        
        // Couldn't find within max width
        return -1f;
    }
    
    public Vector3 FindRoadEntryPoint(Vector3 startOffRoad, Vector3 endOnRoad)
    {
        // Validate inputs
        if (IsPositionOnRoad(startOffRoad) || !IsPositionOnRoad(endOnRoad))
            return Vector3.zero;
            
        // Search along line for entry point
        Vector3 direction = (endOnRoad - startOffRoad).normalized;
        float maxDistance = Vector3.Distance(startOffRoad, endOnRoad);
        float step = roadEdgeDetectionStep;
        float currentDistance = step;
        
        Vector3 lastTestedPoint = startOffRoad;
        bool wasLastPointOnRoad = false;
        
        while (currentDistance <= maxDistance)
        {
            Vector3 testPoint = startOffRoad + direction * currentDistance;
            bool isOnRoad = IsPositionOnRoad(testPoint);
            
            // Transition from off-road to on-road
            if (isOnRoad && !wasLastPointOnRoad)
            {
                // Do binary search for more precise point
                Vector3 preciseEntry = BinarySearchForEdge(lastTestedPoint, testPoint);
                if (preciseEntry != Vector3.zero)
                    return preciseEntry;
                    
                // Fallback
                return testPoint;
            }
            
            lastTestedPoint = testPoint;
            wasLastPointOnRoad = isOnRoad;
            currentDistance += step;
        }
        
        return Vector3.zero;
    }
    
    public Vector3 FindRoadExitPoint(Vector3 startOnRoad, Vector3 endOffRoad)
    {
        // Validate inputs
        if (!IsPositionOnRoad(startOnRoad) || IsPositionOnRoad(endOffRoad))
            return Vector3.zero;
            
        // Search along line for exit point
        Vector3 direction = (endOffRoad - startOnRoad).normalized;
        float maxDistance = Vector3.Distance(startOnRoad, endOffRoad);
        float step = roadEdgeDetectionStep;
        float currentDistance = step;
        
        Vector3 lastTestedPoint = startOnRoad;
        bool wasLastPointOnRoad = true;
        
        while (currentDistance <= maxDistance)
        {
            Vector3 testPoint = startOnRoad + direction * currentDistance;
            bool isOnRoad = IsPositionOnRoad(testPoint);
            
            // Transition from on-road to off-road
            if (!isOnRoad && wasLastPointOnRoad)
            {
                // Do binary search for more precise point
                Vector3 preciseExit = BinarySearchForEdge(lastTestedPoint, testPoint);
                if (preciseExit != Vector3.zero)
                    return preciseExit;
                    
                // Fallback
                return lastTestedPoint;
            }
            
            lastTestedPoint = testPoint;
            wasLastPointOnRoad = isOnRoad;
            currentDistance += step;
        }
        
        return Vector3.zero;
    }
    
    private Vector3 BinarySearchForEdge(Vector3 pointA, Vector3 pointB)
    {
        // Verify one point is on road and one is off
        bool aOnRoad = IsPositionOnRoad(pointA);
        bool bOnRoad = IsPositionOnRoad(pointB);
        
        if (aOnRoad == bOnRoad)
            return Vector3.zero;
            
        // Maximum iterations to prevent infinite loops
        int maxIterations = 10;
        
        for (int i = 0; i < maxIterations; i++)
        {
            // Find midpoint
            Vector3 midPoint = (pointA + pointB) / 2f;
            bool midOnRoad = IsPositionOnRoad(midPoint);
            
            // If tolerance reached
            if (Vector3.Distance(pointA, pointB) < 0.1f)
            {
                return aOnRoad ? pointA : pointB;
            }
            
            // Update search area
            if (midOnRoad == aOnRoad)
            {
                pointA = midPoint;
            }
            else
            {
                pointB = midPoint;
            }
        }
        
        // Return the on-road point
        return aOnRoad ? pointA : pointB;
    }
}