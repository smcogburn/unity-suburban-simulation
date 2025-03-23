using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
    
// Manages path finding, checkpoints, and navigation
public class PathfindingManager
{
    private AgentBehavior agent;
    private NavMeshAgent navAgent;
    
    // Path and checkpoint data
    private List<Vector3> checkpoints = new List<Vector3>();
    private int currentCheckpointIndex = 0;
    private Vector3 finalDestination;
    private bool reachedFinalDestination = true;
    
    // Road analysis helper
    private RoadAnalysis roadAnalysis;
    
    // Settings
    public float checkpointDistance = 5f;
    public float intersectionDetectionRadius = 3f;
    public float lookaheadDistance = 2f;
    
    // Events
    public delegate void CheckpointEvent(Vector3 position, bool isFinal, float remainingDistance);
    public event CheckpointEvent OnCheckpointReached;
    
    public PathfindingManager(AgentBehavior agentBehavior, NavMeshAgent navMeshAgent)
    {
        agent = agentBehavior;
        navAgent = navMeshAgent;
        
        // Create helper
        roadAnalysis = new RoadAnalysis(navMeshAgent);
    }
    
    public void SetDestination(Vector3 targetPosition)
    {
        reachedFinalDestination = false;
        finalDestination = targetPosition;
        
        // Clear previous checkpoints
        checkpoints.Clear();
        currentCheckpointIndex = 0;
        
        // Calculate path
        NavMeshPath path = new NavMeshPath();
        if (navAgent.CalculatePath(targetPosition, path))
        {
            // Generate optimized checkpoints along the path
            GenerateCheckpoints(path);
        }
        else
        {
            Debug.LogWarning("Failed to calculate path to destination");
        }
        
        // Process first checkpoint
        if (checkpoints.Count > 0)
        {
            ProcessNextCheckpoint();
        }
    }
    
    private void GenerateCheckpoints(NavMeshPath path)
    {
        // Start with the agent's current position
        checkpoints.Add(agent.transform.position);
        
        // Process corners from NavMesh path
        Vector3 lastPoint = agent.transform.position;
        
        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 corner = path.corners[i];
            
            // Check road properties for this path segment
            bool lastPointOnRoad = roadAnalysis.IsPositionOnRoad(lastPoint);
            bool cornerOnRoad = roadAnalysis.IsPositionOnRoad(corner);
            bool isIntersection = roadAnalysis.IsIntersection(corner);
            
            // Generate appropriate checkpoints based on terrain and path context
            GeneratePathSegment(lastPoint, corner, lastPointOnRoad, cornerOnRoad, isIntersection, i < path.corners.Length - 1 ? path.corners[i + 1] : finalDestination);
            
            lastPoint = corner;
        }
        
        // Make sure final destination is included
        if (checkpoints.Count == 0 || Vector3.Distance(checkpoints[checkpoints.Count - 1], finalDestination) > 0.1f)
        {
            checkpoints.Add(finalDestination);
        }
        
        Debug.Log($"Generated {checkpoints.Count} checkpoints for path");
    }
    
    private void GeneratePathSegment(Vector3 start, Vector3 end, bool startOnRoad, bool endOnRoad, bool isIntersection, Vector3 nextPoint)
    {
        float distance = Vector3.Distance(start, end);
        
        // Both points on road - special road-following path
        if (startOnRoad && endOnRoad)
        {
            // Center align points for road travel
            Vector3 roadAlignedStart = roadAnalysis.GetRoadCenterPoint(start);
            Vector3 roadAlignedEnd = roadAnalysis.GetRoadCenterPoint(end);
            
            // If at intersection, add detailed intersection handling
            if (isIntersection)
            {
                // Add approach to intersection (if not too close)
                if (distance > intersectionDetectionRadius * 2)
                {
                    float approachDistance = Mathf.Min(distance - intersectionDetectionRadius, checkpointDistance);
                    Vector3 approachDir = (roadAlignedEnd - roadAlignedStart).normalized;
                    Vector3 approachPoint = roadAlignedStart + approachDir * approachDistance;
                    checkpoints.Add(roadAnalysis.GetRoadCenterPoint(approachPoint));
                }
                
                // Add the intersection center point
                checkpoints.Add(roadAlignedEnd);
                
                // Check if next point is also on road for exit path
                bool nextOnRoad = roadAnalysis.IsPositionOnRoad(nextPoint);
                if (nextOnRoad)
                {
                    Vector3 nextAligned = roadAnalysis.GetRoadCenterPoint(nextPoint);
                    Vector3 exitDir = (nextAligned - roadAlignedEnd).normalized;
                    float exitDistance = Mathf.Min(intersectionDetectionRadius, Vector3.Distance(roadAlignedEnd, nextAligned) * 0.3f);
                    
                    // Only add if not too close to other points
                    if (exitDistance > 1.0f)
                    {
                        Vector3 exitPoint = roadAlignedEnd + exitDir * exitDistance;
                        checkpoints.Add(roadAnalysis.GetRoadCenterPoint(exitPoint));
                    }
                }
            }
            // For regular road segments, add intermediate points to ensure road following
            else if (distance > checkpointDistance)
            {
                int divisions = Mathf.CeilToInt(distance / checkpointDistance);
                
                for (int i = 1; i < divisions; i++)
                {
                    float t = i / (float)divisions;
                    Vector3 intermediatePoint = Vector3.Lerp(roadAlignedStart, roadAlignedEnd, t);
                    checkpoints.Add(roadAnalysis.GetRoadCenterPoint(intermediatePoint));
                }
                
                // Add the end point
                checkpoints.Add(roadAlignedEnd);
            }
            else
            {
                // For short segments, just add the end
                checkpoints.Add(roadAlignedEnd);
            }
        }
        // Transitioning between road and non-road
        else if (startOnRoad != endOnRoad)
        {
            // Entering road from non-road
            if (!startOnRoad && endOnRoad)
            {
                // Find where we first enter the road
                Vector3 direction = (end - start).normalized;
                Vector3 roadEntry = roadAnalysis.FindRoadEntryPoint(start, end);
                
                // Add entry point and centered end point
                if (roadEntry != Vector3.zero)
                {
                    checkpoints.Add(roadEntry);
                    checkpoints.Add(roadAnalysis.GetRoadCenterPoint(end));
                }
                else
                {
                    // Fallback if road entry detection fails
                    checkpoints.Add(end);
                }
            }
            // Exiting road to non-road
            else if (startOnRoad && !endOnRoad)
            {
                // Find where we exit the road
                Vector3 roadExit = roadAnalysis.FindRoadExitPoint(start, end);
                
                // Add centered start and exit point
                if (roadExit != Vector3.zero)
                {
                    checkpoints.Add(roadAnalysis.GetRoadCenterPoint(start));
                    checkpoints.Add(roadExit);
                    checkpoints.Add(end);
                }
                else
                {
                    // Fallback if road exit detection fails
                    checkpoints.Add(end);
                }
            }
        }
        // Both points off-road - simple direct path
        else
        {
            // For completely off-road segments, just add the end point
            checkpoints.Add(end);
        }
    }
    
    private void ProcessNextCheckpoint()
    {
        // Safety check
        if (currentCheckpointIndex >= checkpoints.Count)
        {
            reachedFinalDestination = true;
            return;
        }
        
        Vector3 targetCheckpoint = checkpoints[currentCheckpointIndex];
        bool isFinalCheckpoint = (currentCheckpointIndex == checkpoints.Count - 1);
        
        // For intermediate points, use small stopping distance
        navAgent.stoppingDistance = isFinalCheckpoint ? 0.5f : 0.1f;
        
        // Set navigation target
        navAgent.SetDestination(targetCheckpoint);
        
        // Calculate remaining distance to final destination
        float remainingDistance = 0;
        for (int i = currentCheckpointIndex; i < checkpoints.Count - 1; i++)
        {
            remainingDistance += Vector3.Distance(checkpoints[i], checkpoints[i + 1]);
        }
        
        // Notify listeners about the new checkpoint
        OnCheckpointReached?.Invoke(targetCheckpoint, isFinalCheckpoint, remainingDistance);
        
        Debug.Log($"Moving to checkpoint {currentCheckpointIndex}/{checkpoints.Count-1}");
    }
    
    public void Update()
    {
        // Skip if we're already at destination or waiting for path
        if (reachedFinalDestination || navAgent.pathPending)
            return;
            
        // Check for reaching next checkpoint using lookahead
        if (currentCheckpointIndex < checkpoints.Count - 1 &&
            Vector3.Distance(agent.transform.position, checkpoints[currentCheckpointIndex]) <= lookaheadDistance)
        {
            // Advance to next checkpoint
            currentCheckpointIndex++;
            ProcessNextCheckpoint();
        }
        // Check if reached final destination
        else if (currentCheckpointIndex == checkpoints.Count - 1 && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            reachedFinalDestination = true;
            // Notify listeners we've arrived
            OnCheckpointReached?.Invoke(finalDestination, true, 0);
        }
    }
    
    public List<Vector3> GetCheckpoints()
    {
        return checkpoints;
    }
    
    public int GetCurrentCheckpointIndex()
    {
        return currentCheckpointIndex;
    }
    
    public bool HasReachedDestination()
    {
        return reachedFinalDestination;
    }
}