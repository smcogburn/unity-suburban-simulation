// Main agent controller that coordinates all components
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;


// Transport mode enum
public enum TransportMode
{
    Walking,
    Driving,
    Transit
}

// Manages transport modes and movement parameters
public class TransportManager
{
    private AgentBehavior agent;
    private NavMeshAgent navAgent;
    private TransportState currentState;
    
    // Transport state instances
    private WalkingState walkingState;
    private DrivingState drivingState;
    
    // Events
    public delegate void TransportModeChanged(TransportMode newMode);
    public event TransportModeChanged OnTransportModeChanged;
    
    // Settings
    public float longDistanceThreshold = 10f;
    
    public TransportMode CurrentMode { get; private set; }
    
    public TransportManager(AgentBehavior agentBehavior, NavMeshAgent navMeshAgent)
    {
        agent = agentBehavior;
        navAgent = navMeshAgent;
        
        // Create states
        walkingState = new WalkingState(navMeshAgent);
        drivingState = new DrivingState(navMeshAgent);
        
        // Default state
        currentState = walkingState;
        CurrentMode = TransportMode.Walking;
    }
    
    public void SetTransportMode(TransportMode mode)
    {
        // Skip if already in this mode
        if (mode == CurrentMode)
            return;
            
        // Exit current state
        currentState.Exit();
        
        // Update state and mode
        switch (mode)
        {
            case TransportMode.Walking:
                currentState = walkingState;
                break;
                
            case TransportMode.Driving:
                currentState = drivingState;
                break;
                
            case TransportMode.Transit:
                // Future implementation
                currentState = walkingState; // Fallback
                break;
        }
        
        // Enter new state
        currentState.Enter();
        
        // Update current mode
        CurrentMode = mode;
        
        // Notify listeners
        OnTransportModeChanged?.Invoke(mode);
        
        Debug.Log($"Transport mode changed to {mode}");
    }
    
    public void EvaluateTransportMode(Vector3 targetPosition, bool isFinalCheckpoint, float remainingDistance)
    {
        // For final destination, always walk
        if (isFinalCheckpoint)
        {
            if (CurrentMode != TransportMode.Walking)
            {
                SetTransportMode(TransportMode.Walking);
            }
            return;
        }
        
        // Get road status
        bool currentPositionOnRoad = IsOnRoad();
        bool targetPositionOnRoad = IsPositionOnRoad(targetPosition);
        
        // Decide transport mode based on context
        if (currentPositionOnRoad && targetPositionOnRoad && remainingDistance > longDistanceThreshold)
        {
            // Long distance on road - drive
            if (CurrentMode != TransportMode.Driving)
            {
                SetTransportMode(TransportMode.Driving);
            }
        }
        else if (!targetPositionOnRoad || remainingDistance <= longDistanceThreshold)
        {
            // Off-road or short distance - walk
            if (CurrentMode != TransportMode.Walking)
            {
                SetTransportMode(TransportMode.Walking);
            }
        }
    }
    
    public void Update()
    {
        // Let current state update
        currentState.Update();
        
        // Safety check: If driving but off-road, switch to walking
        if (CurrentMode == TransportMode.Driving && !IsOnRoad())
        {
            Debug.Log("Left road while driving - switching to Walking mode");
            SetTransportMode(TransportMode.Walking);
        }
    }
    
    private bool IsOnRoad()
    {
        // Check if agent is on a road
        NavMeshHit hit;
        if (NavMesh.SamplePosition(agent.transform.position, out hit, 0.5f, NavMesh.AllAreas))
        {
            int roadAreaMask = 1 << 3; // Assuming road is area 3
            return (hit.mask & roadAreaMask) != 0;
        }
        return false;
    }
    
    private bool IsPositionOnRoad(Vector3 position)
    {
        // Check if a position is on a road
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            int roadAreaMask = 1 << 3; // Assuming road is area 3
            return (hit.mask & roadAreaMask) != 0;
        }
        return false;
    }
}

// Base class for transport states
public abstract class TransportState
{
    protected NavMeshAgent navAgent;
    
    public TransportState(NavMeshAgent agent)
    {
        navAgent = agent;
    }
    
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

// Walking state implementation
public class WalkingState : TransportState
{
    // Movement parameters
    private float speed = 3.5f;
    private float acceleration = 8f;
    
    // Area costs
    private float walkableCost = 1.0f;
    private float roadCost = 5.0f;
    
    public WalkingState(NavMeshAgent agent) : base(agent) { }
    
    public override void Enter()
    {
        // Set movement parameters
        navAgent.speed = speed;
        navAgent.acceleration = acceleration;
        
        // Set area costs to prefer walkable areas over roads
        navAgent.SetAreaCost(0, walkableCost); // Walkable area
        navAgent.SetAreaCost(3, roadCost);     // Road area
    }
    
    public override void Update()
    {
        // Any ongoing walking logic
    }
    
    public override void Exit()
    {
        // Cleanup or state transition logic
    }
}

// Driving state implementation
public class DrivingState : TransportState
{
    // Movement parameters
    private float speed = 10f;
    private float acceleration = 20f;
    
    // Area costs
    private float walkableCost = 10.0f;
    private float roadCost = 1.0f;
    
    public DrivingState(NavMeshAgent agent) : base(agent) { }
    
    public override void Enter()
    {
        // Set movement parameters
        navAgent.speed = speed;
        navAgent.acceleration = acceleration;
        
        // Set area costs to strongly prefer roads over walkable areas
        navAgent.SetAreaCost(0, walkableCost); // Walkable area (avoid)
        navAgent.SetAreaCost(3, roadCost);     // Road area (prefer)
    }
    
    public override void Update()
    {
        // Any ongoing driving logic
    }
    
    public override void Exit()
    {
        // Cleanup or state transition logic
    }
}