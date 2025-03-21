// AgentBehavior.cs - Main agent controller
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AgentBehavior : MonoBehaviour
{
    [Header("References")]
    public Transform destination;
    
    [Header("Debug Settings")]
    public bool drawDebugVisuals = true;
    [Tooltip("Logs detailed debug information about pathfinding")]
    public bool verboseLogging = false;
    
    // Component references
    private PathfindingManager pathfindingManager;
    private TransportManager transportManager;
    private VisualManager visualManager;
    private NavMeshAgent agent;
    
    [Header("Statistics")]
    [Tooltip("Total distance traveled")]
    public float distanceTraveled;
    [Tooltip("Time spent walking")]
    public float timeWalking;
    [Tooltip("Time spent driving")]
    public float timeDriving;
    [Tooltip("Time spent in congestion")]
    public float timeInCongestion;
    
    private Vector3 lastPosition;
    private TransportMode lastMode;
    private float congestionTimer;
    
    private void Awake()
    {
        // Get NavMeshAgent component
        agent = GetComponent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing from Agent!");
            return;
        }
        
        // Initialize managers
        pathfindingManager = new PathfindingManager(this, agent);
        transportManager = new TransportManager(this, agent);
        visualManager = new VisualManager(this, GetComponentInChildren<Renderer>(), pathfindingManager, transportManager);
        
        // Register event listeners between components
        pathfindingManager.OnWaypointReached += transportManager.OnWaypointReached;
        transportManager.OnTransportModeChanged += visualManager.UpdateVisuals;
        
        // Initialize tracking variables
        lastPosition = transform.position;
        lastMode = TransportMode.Walking;
        distanceTraveled = 0f;
        timeWalking = 0f;
        timeDriving = 0f;
        timeInCongestion = 0f;
    }
    
    private void Start()
    {
        // Initialize with default mode
        transportManager.SetTransportMode(TransportMode.Walking);
        
        // Set initial destination if provided
        if (destination != null)
        {
            SetDestination(destination.position);
        }
    }
    
    // Public method to set a new destination
    public void SetDestination(Vector3 targetPosition)
    {
        LogDebug($"Setting destination to {targetPosition}");
        pathfindingManager.SetDestination(targetPosition);
    }
    
    private void Update()
    {
        // Only proceed if all components are initialized
        if (pathfindingManager == null || transportManager == null || visualManager == null)
            return;
        
        // Handle user input to set destination
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                SetDestination(hit.point);
            }
        }
        
        // Update pathing logic
        pathfindingManager.Update();
        
        // Update transport logic
        transportManager.Update();
        
        // Update statistics
        UpdateAgentStatistics();
        
        // Update visuals
        if (drawDebugVisuals)
        {
            visualManager.DrawDebugVisuals();
        }
    }
    
    private void UpdateAgentStatistics()
    {
        // Calculate distance traveled this frame
        float frameDist = Vector3.Distance(transform.position, lastPosition);
        distanceTraveled += frameDist;
        
        // Track time spent in different transport modes
        TransportMode currentMode = transportManager.CurrentMode;
        if (currentMode == TransportMode.Walking)
        {
            timeWalking += Time.deltaTime;
        }
        else if (currentMode == TransportMode.Driving)
        {
            timeDriving += Time.deltaTime;
            
            // Check if we're in congestion
            if (IsInCongestion())
            {
                timeInCongestion += Time.deltaTime;
                congestionTimer += Time.deltaTime;
                
                // If congestion persists, consider replanning
                if (congestionTimer > 5.0f)
                {
                    congestionTimer = 0f;
                    // Could add logic here to replan route if congestion is bad
                }
            }
            else
            {
                congestionTimer = 0f;
            }
        }
        
        // Update for next frame
        lastPosition = transform.position;
        lastMode = currentMode;
    }
    
    // Method to check if agent is currently in congested traffic
    private bool IsInCongestion()
    {
        // Check if we're on a road
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 0.5f, 1 << 3))
        {
            // Find the road segment we're on
            RoadSegment[] roadSegments = FindObjectsOfType<RoadSegment>();
            
            foreach (var road in roadSegments)
            {
                if (road == null) continue;
                
                // Define bounds of the road (assuming roads are box-shaped)
                Bounds roadBounds = new Bounds(road.transform.position, road.transform.localScale);
                
                // Check if agent is within the road bounds
                if (roadBounds.Contains(transform.position))
                {
                    // Check congestion level
                    if (road.congestionLevel > 0.5f)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    // Method to notify agent of congestion changes
    public void NotifyCongestionChanged(RoadSegment congestedRoad)
    {
        // We could implement more sophisticated behavior here
        LogDebug($"Agent notified of congestion on road segment {congestedRoad.name}");
        
        // If congestion is severe and we're going to use this road, consider replanning
        if (congestedRoad.congestionLevel > 0.8f)
        {
            // Could add logic to replan route
            // For now, just log it
            LogDebug("Severe congestion detected - would consider replanning");
        }
    }
    
    // Debug log helper that respects verbose setting
    private void LogDebug(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[Agent {name}] {message}");
        }
    }
}