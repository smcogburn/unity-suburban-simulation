// RoadNetworkSetup.cs - attach to a GameObject in your scene
using UnityEngine;
using UnityEngine.AI;

public class RoadNetworkSetup : MonoBehaviour
{
    [Header("Road Network Setup")]
    [Tooltip("Layer mask to identify objects that should be marked as roads")]
    public LayerMask roadLayerMask;
    
    [Tooltip("The NavMesh area index for roads")]
    public int roadAreaIndex = 3;
    
    [Header("Road Network Debug")]
    public bool showRoadConnections = true;
    
    private RoadSegment[] roadSegments;
    
    void Start()
    {
        // Find all road segments in the scene
        roadSegments = FindObjectsOfType<RoadSegment>();
        
        if (roadSegments.Length == 0)
        {
            Debug.LogWarning("No RoadSegment components found in the scene. Road network will not function properly.");
        }
        else
        {
            Debug.Log($"Found {roadSegments.Length} road segments in the scene.");
        }
    }
    
    // This method should be called from your editor script after updating roads
    public void UpdateRoadConnections()
    {
        roadSegments = FindObjectsOfType<RoadSegment>();
        
        foreach (var road in roadSegments)
        {
            // Flag this road segment to update its connections on next frame
            if (road != null)
            {
                road.FindConnections(roadSegments);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showRoadConnections) return;
        
        // Find all road segments in the scene
        RoadSegment[] roads = FindObjectsOfType<RoadSegment>();
        
        // Draw connections between road segments
        foreach (var road in roads)
        {
            if (road == null) continue;
            
            // Draw road center
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(road.transform.position, 0.5f);
            
            // Draw connections to other roads
            Gizmos.color = Color.yellow;
            foreach (var connection in road.connectedRoads)
            {
                if (connection != null)
                {
                    Gizmos.DrawLine(road.transform.position, connection.transform.position);
                }
            }
        }
    }
    
    // Utility method to check if a position is on a road
    public static bool IsPositionOnRoad(Vector3 position, int roadAreaIndex)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            int roadMask = 1 << roadAreaIndex;
            return (hit.mask & roadMask) != 0;
        }
        return false;
    }
    
    // Utility method to find the nearest road point to a position
    public static Vector3 FindNearestRoadPoint(Vector3 position, int roadAreaIndex, float maxDistance = 10f)
    {
        NavMeshHit hit;
        int roadMask = 1 << roadAreaIndex;
        
        if (NavMesh.SamplePosition(position, out hit, maxDistance, roadMask))
        {
            return hit.position;
        }
        
        return position; // Return original position if no road found
    }
}