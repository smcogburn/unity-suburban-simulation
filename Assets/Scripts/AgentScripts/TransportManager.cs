// TransportManager.cs - Manages transport modes and movement parameters
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
    
    public TransportMode CurrentMode { get; private set; }
    
    // Constructor
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
    
    // Set the transport mode
    public void SetTransportMode(TransportMode mode)
    {
        // Skip if already in this mode
        if (mode == CurrentMode)
            return;
        
        Debug.Log($"Changing transport mode from {CurrentMode} to {mode}");
        
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
    }
    
    // Called when reaching a waypoint
    public void OnWaypointReached(Vector3 position, bool isFinal, TransportMode mode)
    {
        // Set the appropriate transport mode for this segment
        SetTransportMode(mode);
    }
    
    // Update method called every frame
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
    
    // Check if agent is on a road
    private bool IsOnRoad()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(agent.transform.position, out hit, 0.5f, NavMesh.AllAreas))
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
    private float angularSpeed = 120f;
    
    // Area costs
    private float walkableCost = 1.0f;
    private float roadCost = 5.0f;
    
    public WalkingState(NavMeshAgent agent) : base(agent) { }
    
    public override void Enter()
    {
        // Set movement parameters
        navAgent.speed = speed;
        navAgent.acceleration = acceleration;
        navAgent.angularSpeed = angularSpeed;
        
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
    private float speed = 30f;
    private float acceleration = 20f;
    private float angularSpeed = 80f; // Slower turning when driving
    
    // Area costs
    private float walkableCost = 10.0f;
    private float roadCost = 1.0f;
    
    public DrivingState(NavMeshAgent agent) : base(agent) { }
    
    public override void Enter()
    {
        // Set movement parameters
        navAgent.speed = speed;
        navAgent.acceleration = acceleration;
        navAgent.angularSpeed = angularSpeed;
        
        // Set area costs to strongly prefer roads over walkable areas
        navAgent.SetAreaCost(0, walkableCost); // Walkable area (avoid)
        navAgent.SetAreaCost(3, roadCost);     // Road area (prefer)
    }
    
    public override void Update()
    {
        // Any ongoing driving logic - could add more realistic behavior here
        // Like staying in lanes, following traffic rules, etc.
    }
    
    public override void Exit()
    {
        // Cleanup or state transition logic
    }
}