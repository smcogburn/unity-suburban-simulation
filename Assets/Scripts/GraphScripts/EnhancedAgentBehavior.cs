using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enhanced agent behavior that uses the transport network
/// </summary>
public class EnhancedAgentBehavior : MonoBehaviour
{
    [Header("Network References")]
    public TransportNetwork network;
    
    [Header("Navigation")]
    public NavMeshAgent navAgent;
    public float waypointReachedDistance = 1.5f;
    
    [Header("Transport Modes")]
    public TransportModeType preferredMode = TransportModeType.Mixed;
    public float walkSpeed = 1.4f;  // m/s (~5 km/h)
    public float driveSpeed = 8.3f; // m/s (~30 km/h)
    public float bikeSpeed = 4.0f;  // m/s (~14 km/h)
    
    [Header("Debug")]
    public bool showPath = true;
    public Color pathColor = Color.green;
    public Color walkPathColor = Color.blue;
    public Color drivePathColor = Color.red;
    
    // Path data
    private List<TransportNode> currentPath = new List<TransportNode>();
    private int currentNodeIndex = 0;
    private TransportModeType currentMode = TransportModeType.Walking;
    private bool isMoving = false;
    private Vector3 finalDestination;
    
    // Multi-phase journey data
    private enum JourneyPhase { Initial, WalkToRoad, OnRoad, WalkToDestination, Complete }
    private JourneyPhase currentPhase = JourneyPhase.Initial;
    private Vector3 walkToRoadTarget;
    private Vector3 walkToDestinationStart;
    
    // Start is called before the first frame update
    void Start()
    {
        // Get references if not set
        if (navAgent == null)
        {
            navAgent = GetComponent<NavMeshAgent>();
        }
        
        if (network == null)
        {
            network = FindObjectOfType<TransportNetwork>();
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // Skip if not moving
        if (!isMoving)
            return;
            
        // Handle different phases of the journey
        switch (currentPhase)
        {
            case JourneyPhase.WalkToRoad:
                // Check if we've reached the road entry point
                float distToRoad = Vector3.Distance(transform.position, walkToRoadTarget);
                if (distToRoad <= waypointReachedDistance)
                {
                    Debug.Log("Reached road entry point, switching to road travel");
                    currentPhase = JourneyPhase.OnRoad;
                    currentNodeIndex = 0;
                    ProcessNextWaypoint();
                }
                break;
                
            case JourneyPhase.OnRoad:
                // Check if we've reached the current waypoint
                if (currentNodeIndex < currentPath.Count)
                {
                    Vector3 targetPos = currentPath[currentNodeIndex].Position;
                    float distance = Vector3.Distance(transform.position, targetPos);
                    
                    if (distance <= waypointReachedDistance)
                    {
                        // Move to next waypoint
                        currentNodeIndex++;
                        
                        if (currentNodeIndex < currentPath.Count)
                        {
                            ProcessNextWaypoint();
                        }
                        else
                        {
                            // Reached the point where we need to exit the road
                            Debug.Log("Reached road exit point, switching to walking to destination");
                            walkToDestinationStart = transform.position;
                            currentPhase = JourneyPhase.WalkToDestination;
                            currentMode = TransportModeType.Walking;
                            SetAgentSpeed(currentMode);
                            navAgent.SetDestination(finalDestination);
                        }
                    }
                }
                break;
                
            case JourneyPhase.WalkToDestination:
                // Check if we've reached the final destination
                float distToDest = Vector3.Distance(transform.position, finalDestination);
                if (distToDest <= waypointReachedDistance)
                {
                    Debug.Log("Reached final destination");
                    OnDestinationReached();
                }
                break;
        }
    }
    
    /// <summary>
    /// Set a new destination for the agent
    /// </summary>
    public void SetDestination(Vector3 targetPosition)
    {
        Debug.Log($"Setting destination to {targetPosition}");
        finalDestination = targetPosition;
        
        // First find ANY node closest to our position (not limited to walking)
        TransportNode nearestNode = network.FindClosestNode(transform.position, TransportModeType.Mixed, 50f);
        
        if (nearestNode == null)
        {
            Debug.LogWarning("Could not find any node near agent position");
            return;
        }
        
        Debug.Log($"Found nearest node of type {nearestNode.Type} at {nearestNode.Position}");
        
        // Then find suitable road node near destination
        TransportNode destinationNode = network.FindClosestNode(targetPosition, preferredMode);
        
        if (destinationNode == null)
        {
            Debug.LogWarning("Could not find suitable node near destination");
            return;
        }
        
        Debug.Log($"Found destination node of type {destinationNode.Type} at {destinationNode.Position}");
        
        // Three phase journey:
        // 1. Walk to nearest road node
        // 2. Use road network to travel
        // 3. Walk from road to final destination
        
        // Phase 1: Walk to nearest road node
        currentPhase = JourneyPhase.WalkToRoad;
        walkToRoadTarget = nearestNode.Position;
        currentMode = TransportModeType.Walking;
        SetAgentSpeed(currentMode);
        navAgent.SetDestination(walkToRoadTarget);
        isMoving = true;
        
        Debug.Log($"Walking to nearest transport node at {walkToRoadTarget}");
        
        // Phase 2: Find path through road network
        currentPath = network.FindPath(nearestNode, destinationNode, preferredMode);
        
        if (currentPath.Count == 0)
        {
            Debug.LogWarning("Could not find path through network");
            // Continue with direct NavMesh path as fallback
            navAgent.SetDestination(targetPosition);
            currentPhase = JourneyPhase.WalkToDestination;
            return;
        }
        
        Debug.Log($"Found network path with {currentPath.Count} nodes from {nearestNode.Position} to {destinationNode.Position}");
    }
    
    /// <summary>
    /// Process the next waypoint in the path
    /// </summary>
    private void ProcessNextWaypoint()
    {
        // Make sure we have a valid index
        if (currentNodeIndex >= currentPath.Count)
        {
            Debug.LogWarning("Invalid node index");
            return;
        }
        
        // Get current target node
        TransportNode targetNode = currentPath[currentNodeIndex];
        
        // Get transport mode based on the edge we're about to traverse
        DetermineTransportMode();
        
        // Update agent speed based on transport mode
        SetAgentSpeed(currentMode);
        
        // Set NavMesh destination to node position
        navAgent.SetDestination(targetNode.Position);
        
        Debug.Log($"Moving to node {currentNodeIndex + 1}/{currentPath.Count} using {currentMode} mode");
    }
    
    /// <summary>
    /// Determine which transport mode to use for the current path segment
    /// </summary>
    private void DetermineTransportMode()
    {
        // Starting point or last point
        if (currentNodeIndex >= currentPath.Count - 1)
        {
            currentMode = TransportModeType.Walking;
            return;
        }
        
        // Get current and next nodes
        TransportNode currentNode = currentPath[currentNodeIndex];
        TransportNode nextNode = currentPath[currentNodeIndex + 1];
        
        // Get the edge between them
        TransportEdge edge = currentNode.GetEdgeTo(nextNode);
        
        if (edge == null)
        {
            // Fallback if no edge found
            currentMode = TransportModeType.Walking;
            return;
        }
        
        // Determine mode based on edge
        if (edge.AllowsMode(preferredMode))
        {
            currentMode = preferredMode;
        }
        else
        {
            // Use the first allowed mode on this edge
            foreach (TransportModeType mode in edge.AllowedModes)
            {
                currentMode = mode;
                break;
            }
        }
    }
    
    /// <summary>
    /// Set agent speed based on transport mode
    /// </summary>
    private void SetAgentSpeed(TransportModeType mode)
    {
        switch (mode)
        {
            case TransportModeType.Walking:
                navAgent.speed = walkSpeed;
                break;
            case TransportModeType.Driving:
                navAgent.speed = driveSpeed;
                break;
            case TransportModeType.Biking:
                navAgent.speed = bikeSpeed;
                break;
            case TransportModeType.Transit:
                navAgent.speed = driveSpeed * 0.8f; // Slightly slower than driving
                break;
            default:
                navAgent.speed = walkSpeed;
                break;
        }
    }
    
    /// <summary>
    /// Called when destination is reached
    /// </summary>
    private void OnDestinationReached()
    {
        Debug.Log("Destination reached");
        isMoving = false;
        currentPath.Clear();
        currentPhase = JourneyPhase.Complete;
        
        // Optionally call a delegate/event here to notify other systems
    }
    
    /// <summary>
    /// Visualize the path
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showPath || !isMoving)
            return;
            
        // Draw the initial walking path if in that phase
        if (currentPhase == JourneyPhase.WalkToRoad)
        {
            Gizmos.color = walkPathColor;
            Gizmos.DrawLine(transform.position, walkToRoadTarget);
            Gizmos.DrawSphere(walkToRoadTarget, 0.5f);
        }
        
        // Draw the network path
        if (currentPath != null && currentPath.Count >= 2)
        {
            Gizmos.color = drivePathColor;
            
            // Draw path between nodes
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                if (currentPath[i] != null && currentPath[i + 1] != null)
                {
                    Vector3 start = currentPath[i].Position;
                    Vector3 end = currentPath[i + 1].Position;
                    
                    // Draw line segments
                    Gizmos.DrawLine(start, end);
                    
                    // Draw spheres at each node
                    Gizmos.DrawSphere(start, 0.3f);
                }
            }
            
            // Draw last node
            if (currentPath.Count > 0 && currentPath[currentPath.Count - 1] != null)
            {
                Gizmos.DrawSphere(currentPath[currentPath.Count - 1].Position, 0.3f);
            }
        }
        
        // Draw the final walking path
        if (currentPhase == JourneyPhase.WalkToDestination)
        {
            Gizmos.color = walkPathColor;
            Gizmos.DrawLine(transform.position, finalDestination);
            Gizmos.DrawSphere(finalDestination, 0.5f);
        }
        
        // Draw special marker for current target node
        if (currentPhase == JourneyPhase.OnRoad && currentNodeIndex < currentPath.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentPath[currentNodeIndex].Position, 0.5f);
        }
    }
}