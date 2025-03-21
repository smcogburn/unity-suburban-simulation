// RoadDirectionUtility.cs - Helper class for road direction
using UnityEngine;

public static class RoadDirectionUtility
{
    // Visualize direction vectors to help debug road directions
    public static void VisualizeDirections(Vector3 position, Vector3 forward, Vector3 right, float duration = 3.0f)
    {
        Debug.DrawRay(position, forward * 5.0f, Color.blue, duration);  // Forward as blue
        Debug.DrawRay(position, right * 5.0f, Color.red, duration);     // Right as red
    }
    
    // Detect primary road direction based on scale and rotation
    public static Vector3 GetRoadDirection(Transform roadTransform)
    {
        // Get the road's scale
        Vector3 scale = roadTransform.localScale;
        
        // Output debug info
        Debug.Log($"Road {roadTransform.name} - Scale: {scale}, Rotation: {roadTransform.eulerAngles}");
        
        // First, check if one dimension is clearly longer
        if (scale.x > scale.z * 1.5f)
        {
            Debug.Log($"Road {roadTransform.name} - Using X-axis as primary direction");
            return roadTransform.right;
        }
        else if (scale.z > scale.x * 1.5f)
        {
            Debug.Log($"Road {roadTransform.name} - Using Z-axis as primary direction");
            return roadTransform.forward;
        }
        
        // If dimensions are similar, use the rotation to determine direction
        // Assume roads are primarily aligned with the world X or Z axis
        float xRotAlignment = Mathf.Abs(Vector3.Dot(roadTransform.right, Vector3.right));
        float zRotAlignment = Mathf.Abs(Vector3.Dot(roadTransform.forward, Vector3.forward));
        
        // Use the most aligned axis
        if (xRotAlignment > zRotAlignment)
        {
            Debug.Log($"Road {roadTransform.name} - Using right as primary direction (based on rotation)");
            return roadTransform.right;
        }
        else
        {
            Debug.Log($"Road {roadTransform.name} - Using forward as primary direction (based on rotation)");
            return roadTransform.forward;
        }
    }
    
    // Calculate start point of road
    public static Vector3 GetStartPoint(Transform roadTransform, float length)
    {
        Vector3 direction = GetRoadDirection(roadTransform);
        Vector3 startPoint = roadTransform.position - direction * (length / 2);
        
        // Visualize
        Debug.DrawLine(roadTransform.position, startPoint, Color.yellow, 5.0f);
        
        return startPoint;
    }
    
    // Calculate end point of road
    public static Vector3 GetEndPoint(Transform roadTransform, float length)
    {
        Vector3 direction = GetRoadDirection(roadTransform);
        Vector3 endPoint = roadTransform.position + direction * (length / 2);
        
        // Visualize
        Debug.DrawLine(roadTransform.position, endPoint, Color.yellow, 5.0f);
        
        return endPoint;
    }
}