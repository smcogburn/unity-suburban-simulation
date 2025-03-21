// // NavMeshAreaSetup.cs - attach to the same object as your NavMeshSurface
// using UnityEngine;
// using UnityEditor;

// #if UNITY_EDITOR
// public class NavMeshAreaSetup : MonoBehaviour
// {
//     // This is a utility class to help set up your NavMesh areas correctly
    
//     [Header("NavMesh Area Indices")]
//     public int walkableAreaIndex = 0;
//     public int roadAreaIndex = 3;
//     public int sidewalkAreaIndex = 4;
    
//     [Header("Road Setup")]
//     public LayerMask roadLayerMask;
//     public float roadWidth = 4f;
    
//     [Header("Sidewalk Setup - Future")]
//     public LayerMask sidewalkLayerMask;
//     public float sidewalkWidth = 1.5f;
    
//     // Add a button in the Editor to help configure areas
//     public void SetupNavMeshAreas()
//     {
//         // This would be implemented in an Editor script
//         Debug.Log("This function needs to be called from an Editor script");
//     }
    
//     // Help validate the NavMesh setup
//     void OnValidate()
//     {
//         Debug.Log("NavMesh Area Configuration Guide:");
//         Debug.Log("1. Ensure your NavMeshSurface has the following areas defined:");
//         Debug.Log("   - Area " + walkableAreaIndex + ": Walkable (default area for general movement)");
//         Debug.Log("   - Area " + roadAreaIndex + ": Road (for vehicle movement)");
//         Debug.Log("   - Area " + sidewalkAreaIndex + ": Sidewalk (for pedestrian movement, future)");
//         Debug.Log("2. Tag your road objects with the appropriate layer for automatic area assignment");
//         Debug.Log("3. Rebake your NavMesh after making changes");
//     }
    
//     // Visual debugging in the Scene view
//     void OnDrawGizmosSelected()
//     {
//         // Draw labels for the different NavMesh areas
//         Gizmos.color = Color.blue;
//         Gizmos.DrawWireSphere(transform.position, 1f);
        
// #if UNITY_EDITOR
//         // Draw labels for the NavMesh areas in the Scene view
//         GUIStyle style = new GUIStyle();
//         style.normal.textColor = Color.white;
//         style.fontSize = 14;
//         style.fontStyle = FontStyle.Bold;
        
//         Handles.Label(transform.position + Vector3.up * 1.5f, "NavMesh Areas:", style);
//         style.fontSize = 12;
//         style.fontStyle = FontStyle.Normal;
        
//         Handles.Label(transform.position + Vector3.up * 1.2f, "Walkable: Area " + walkableAreaIndex, style);
//         Handles.Label(transform.position + Vector3.up * 0.9f, "Road: Area " + roadAreaIndex, style);
//         Handles.Label(transform.position + Vector3.up * 0.6f, "Sidewalk: Area " + sidewalkAreaIndex + " (future)", style);
// #endif
//     }
// }
// #endif