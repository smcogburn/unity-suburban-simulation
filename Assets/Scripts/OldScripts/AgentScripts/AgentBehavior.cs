// Main agent controller that coordinates all components
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AgentBehavior : MonoBehaviour
{
    [Header("References")]
    public Transform destination;
    
    [Header("Debug Settings")]
    public bool drawDebugVisuals = true;
    
    // Component references
    private PathfindingManager pathfindingManager;
    private TransportManager transportManager;
    private VisualManager visualManager;
    private NavMeshAgent agent;
    
    private void Awake()
    {
        // Create and initialize component managers
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
        pathfindingManager.OnCheckpointReached += transportManager.EvaluateTransportMode;
        transportManager.OnTransportModeChanged += visualManager.UpdateVisuals;
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
    
    public void SetDestination(Vector3 targetPosition)
    {
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
        
        // Update visuals
        if (drawDebugVisuals)
        {
            visualManager.DrawDebugVisuals();
        }
    }
}