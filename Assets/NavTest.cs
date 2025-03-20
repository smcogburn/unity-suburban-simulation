// BasicNavigation.cs - attach to your agent
using UnityEngine;
using UnityEngine.AI;

public class BasicNavigation : MonoBehaviour
{
    public Transform destination;
    private NavMeshAgent agent;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (destination != null)
            agent.SetDestination(destination.position);
    }
    
    // For testing - click to set new destination
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.SetDestination(hit.point);
            }
        }
    }
}