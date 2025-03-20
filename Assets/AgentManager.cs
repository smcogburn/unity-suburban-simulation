// AgentManager.cs - attach to an empty GameObject
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public GameObject agentPrefab;
    public Transform[] spawnPoints;
    public Transform[] destinationPoints;
    
    [Header("Simulation Settings")]
    public int initialAgentCount = 5;
    public float spawnInterval = 5f;
    
    private List<GameObject> agents = new List<GameObject>();
    private float nextSpawnTime;
    
    void Start()
    {
        // Spawn initial agents
        for (int i = 0; i < initialAgentCount; i++)
        {
            SpawnAgent();
        }
        
        nextSpawnTime = Time.time + spawnInterval;
    }
    
    void Update()
    {
        // Periodic spawning
        // if (Time.time >= nextSpawnTime)
        // {
        //     SpawnAgent();
        //     nextSpawnTime = Time.time + spawnInterval;
        // }
    }
    
    void SpawnAgent()
    {
        if (spawnPoints.Length == 0 || destinationPoints.Length == 0)
            return;
            
        // Get random spawn and destination
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Transform destination = destinationPoints[Random.Range(0, destinationPoints.Length)];
        
        // Create agent
        GameObject agent = Instantiate(agentPrefab, spawn.position, Quaternion.identity);
        
        // Set destination
        AgentBehavior behavior = agent.GetComponent<AgentBehavior>();
        if (behavior != null)
        {
            behavior.destination = destination;
            behavior.SetDestination(destination.position);
        }
        
        agents.Add(agent);
    }
}