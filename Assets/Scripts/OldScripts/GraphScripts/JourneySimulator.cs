using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates journeys through the transport network, including virtual agents
/// </summary>
public class JourneySimulator : MonoBehaviour
{
    [Header("References")]
    public TransportNetwork network;
    
    [Header("Simulation Settings")]
    public int maxActiveJourneys = 100;
    public float journeySpawnRate = 10f; // journeys per minute
    
    [Header("Visualization")]
    public bool visualizeJourneys = true;
    public GameObject agentPrefab;
    public int maxVisualAgents = 20;
    
    // Active journeys
    private List<VirtualJourney> activeJourneys = new List<VirtualJourney>();
    private List<VisualAgent> visualAgents = new List<VisualAgent>();
    
    // Track time for spawning
    private float lastSpawnTime;
    
    // Start is called before the first frame update
    void Start()
    {
        lastSpawnTime = Time.time;
        
        // Create initial visual agents if needed
        if (visualizeJourneys && agentPrefab != null)
        {
            for (int i = 0; i < maxVisualAgents; i++)
            {
                GameObject agentObj = Instantiate(agentPrefab, Vector3.zero, Quaternion.identity);
                agentObj.SetActive(false);
                agentObj.name = $"VisualAgent_{i}";
                
                VisualAgent agent = new VisualAgent();
                agent.gameObject = agentObj;
                agent.active = false;
                
                visualAgents.Add(agent);
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // Spawn new journeys if needed
        if (activeJourneys.Count < maxActiveJourneys)
        {
            float timeSinceLastSpawn = Time.time - lastSpawnTime;
            float spawnInterval = 60f / journeySpawnRate;
            
            if (timeSinceLastSpawn >= spawnInterval)
            {
                SpawnRandomJourney();
                lastSpawnTime = Time.time;
            }
        }
        
        // Update all active journeys
        UpdateJourneys();
    }
    
    /// <summary>
    /// Update all active journeys
    /// </summary>
    private void UpdateJourneys()
    {
        // Copy list to avoid modification issues
        List<VirtualJourney> journeysToUpdate = new List<VirtualJourney>(activeJourneys);
        
        foreach (var journey in journeysToUpdate)
        {
            UpdateJourney(journey, Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Update a single journey
    /// </summary>
    private void UpdateJourney(VirtualJourney journey, float deltaTime)
    {
        // Skip completed journeys
        if (journey.IsComplete)
        {
            activeJourneys.Remove(journey);
            
            // Free visual agent if one was assigned
            if (journey.visualAgent != null)
            {
                journey.visualAgent.active = false;
                journey.visualAgent.gameObject.SetActive(false);
                journey.visualAgent = null;
            }
            
            return;
        }
        
        // Get current edge
        TransportEdge currentEdge = journey.CurrentEdge;
        if (currentEdge == null)
        {
            Debug.LogWarning("Journey has no current edge");
            activeJourneys.Remove(journey);
            return;
        }
        
        // Update journey progress
        float speed = currentEdge.CurrentSpeed;
        journey.progress += (speed * deltaTime) / currentEdge.Length;
        
        // Update visual agent if assigned
        UpdateVisualAgent(journey);
        
        // Check if reached next node
        if (journey.progress >= 1.0f)
        {
            MoveToNextEdge(journey);
        }
    }
    
    /// <summary>
    /// Update visual agent for a journey
    /// </summary>
    private void UpdateVisualAgent(VirtualJourney journey)
    {
        if (!visualizeJourneys || journey.visualAgent == null)
            return;
            
        if (journey.CurrentEdge == null)
            return;
            
        // Calculate position along current edge
        Vector3 position = journey.CurrentEdge.GetPointAlongEdge(journey.progress);
        
        // Update visual agent position
        journey.visualAgent.gameObject.transform.position = position;
        
        // Update color based on transport mode
        Renderer renderer = journey.visualAgent.gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color modeColor = GetColorForMode(journey.currentMode);
            renderer.material.color = modeColor;
        }
        
        // Update rotation to face direction of travel
        if (journey.progress < 0.95f)
        {
            Vector3 nextPos = journey.CurrentEdge.GetPointAlongEdge(journey.progress + 0.05f);
            Vector3 direction = (nextPos - position).normalized;
            
            if (direction != Vector3.zero)
            {
                journey.visualAgent.gameObject.transform.forward = direction;
            }
        }
    }
    
    /// <summary>
    /// Move a journey to its next edge
    /// </summary>
    private void MoveToNextEdge(VirtualJourney journey)
    {
        // Unregister from current edge
        if (journey.CurrentEdge != null)
        {
            journey.CurrentEdge.UnregisterAgent();
        }
        
        // Move to next path segment
        journey.currentPathIndex++;
        
        // Check if journey is complete
        if (journey.currentPathIndex >= journey.path.Count - 1)
        {
            journey.IsComplete = true;
            Debug.Log("Journey completed");
            return;
        }
        
        // Get new current edge
        TransportNode currentNode = journey.path[journey.currentPathIndex];
        TransportNode nextNode = journey.path[journey.currentPathIndex + 1];
        
        TransportEdge nextEdge = currentNode.GetEdgeTo(nextNode);
        if (nextEdge == null)
        {
            Debug.LogError("Could not find edge between nodes in path");
            journey.IsComplete = true;
            return;
        }
        
        // Check for mode change
        TransportModeType prevMode = journey.currentMode;
        
        // Determine mode for this edge
        if (!nextEdge.AllowsMode(journey.currentMode))
        {
            // Need to change mode
            foreach (TransportModeType mode in nextEdge.AllowedModes)
            {
                journey.currentMode = mode;
                break;
            }
        }
        
        // Register on new edge
        journey.currentEdge = nextEdge;
        journey.progress = 0f;
        nextEdge.RegisterAgent();
        
        // Log mode changes
        if (prevMode != journey.currentMode)
        {
            Debug.Log($"Journey changing mode from {prevMode} to {journey.currentMode}");
        }
    }
    
    /// <summary>
    /// Spawn a new random journey
    /// </summary>
    private void SpawnRandomJourney()
    {
        // Find two random nodes for start and end
        var nodes = new List<TransportNode>();
        
        // TODO: Get a proper random selection of nodes
        // This is just a placeholder 
        TransportNode startNode = null;
        TransportNode endNode = null;
        
        // Until we have proper node selection working
        if (nodes.Count < 2)
        {
            Debug.LogWarning("Not enough nodes for random journey");
            return;
        }
        
        startNode = nodes[Random.Range(0, nodes.Count)];
        do
        {
            endNode = nodes[Random.Range(0, nodes.Count)];
        } while (endNode == startNode);
        
        // Create a journey between these nodes
        SpawnJourney(startNode, endNode, TransportModeType.Mixed);
    }
    
    /// <summary>
    /// Spawn a journey between specific points
    /// </summary>
    public VirtualJourney SpawnJourney(Vector3 startPos, Vector3 endPos, TransportModeType preferredMode)
    {
        // Find closest nodes
        TransportNode startNode = network.FindClosestNode(startPos, preferredMode);
        TransportNode endNode = network.FindClosestNode(endPos, preferredMode);
        
        if (startNode == null || endNode == null)
        {
            Debug.LogWarning("Could not find nodes for journey");
            return null;
        }
        
        return SpawnJourney(startNode, endNode, preferredMode);
    }
    
    /// <summary>
    /// Spawn a journey between specific nodes
    /// </summary>
    public VirtualJourney SpawnJourney(TransportNode startNode, TransportNode endNode, TransportModeType preferredMode)
    {
        // Find a path
        List<TransportNode> path = network.FindPath(startNode, endNode, preferredMode);
        
        if (path.Count < 2)
        {
            Debug.LogWarning("Could not find path for journey");
            return null;
        }
        
        // Create journey
        VirtualJourney journey = new VirtualJourney();
        journey.path = path;
        journey.currentPathIndex = 0;
        journey.currentMode = preferredMode;
        journey.progress = 0f;
        journey.IsComplete = false;
        
        // Set initial edge
        TransportNode currentNode = path[0];
        TransportNode nextNode = path[1];
        
        TransportEdge edge = currentNode.GetEdgeTo(nextNode);
        if (edge == null)
        {
            Debug.LogError("Could not find edge between nodes in path");
            return null;
        }
        
        journey.currentEdge = edge;
        edge.RegisterAgent();
        
        // Add to active journeys
        activeJourneys.Add(journey);
        
        // Assign visual agent if visualizing
        if (visualizeJourneys)
        {
            AssignVisualAgent(journey);
        }
        
        Debug.Log($"Spawned journey with {path.Count} nodes");
        return journey;
    }
    
    /// <summary>
    /// Assign a visual agent to a journey
    /// </summary>
    private void AssignVisualAgent(VirtualJourney journey)
    {
        // Find an inactive visual agent
        foreach (var agent in visualAgents)
        {
            if (!agent.active)
            {
                // Assign this agent
                agent.active = true;
                agent.gameObject.SetActive(true);
                journey.visualAgent = agent;
                
                // Position at start of journey
                if (journey.CurrentEdge != null)
                {
                    agent.gameObject.transform.position = journey.CurrentEdge.StartNode.Position;
                }
                
                return;
            }
        }
        
        // No available visual agents
        Debug.Log("No available visual agents");
    }
    
    /// <summary>
    /// Get color for a transport mode
    /// </summary>
    private Color GetColorForMode(TransportModeType mode)
    {
        switch (mode)
        {
            case TransportModeType.Walking:
                return Color.green;
            case TransportModeType.Driving:
                return Color.blue;
            case TransportModeType.Transit:
                return Color.yellow;
            case TransportModeType.Biking:
                return Color.cyan;
            default:
                return Color.white;
        }
    }
}

/// <summary>
/// Represents a journey through the transport network
/// </summary>
public class VirtualJourney
{
    // Path through the network
    public List<TransportNode> path;
    public int currentPathIndex;
    
    // Current state
    public TransportEdge currentEdge;
    public TransportModeType currentMode;
    public float progress; // 0-1 along current edge
    
    // Status
    public bool IsComplete;
    
    // Visualization
    public VisualAgent visualAgent;
    
    // Current edge property
    public TransportEdge CurrentEdge => currentEdge;
}

/// <summary>
/// Represents a visual agent in the scene
/// </summary>
public class VisualAgent
{
    public GameObject gameObject;
    public bool active;
}