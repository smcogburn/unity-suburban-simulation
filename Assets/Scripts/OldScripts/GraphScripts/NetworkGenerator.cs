using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

/// <summary>
/// Improved network generator that handles mid-road intersections
/// </summary>
public class ImprovedNetworkGenerator : MonoBehaviour
{
    [Header("Network Reference")]
    public TransportNetwork network;
    
    [Header("Road Generation")]
    public LayerMask roadLayerMask;
    public float nodeSpacing = 5f;
    public float connectionThreshold = 3f;
    
    [Header("Unity NavMesh Integration")]
    public int walkableAreaIndex = 0;   // Default walkable
    public int roadAreaIndex = 3;       // Road area
    
    [Header("Generation Options")]
    public bool generateOnStart = true;
    public bool clearBeforeGeneration = true;
    public bool createNodesAtRoadEnds = true;
    public bool connectIntersections = true;
    
    [Header("Improved Intersection Detection")]
    public bool findCrossingIntersections = true;
    public bool debugIntersectionDetection = true;
    
    // Store road data for intersection detection
    private List<RoadData> roadDataList = new List<RoadData>();
    
    // Class to store road segment data
    private class RoadData
    {
        public GameObject roadObject;
        public Vector3 startPoint;
        public Vector3 endPoint;
        public Vector3 direction;
        public float length;
        public float width;
        public List<TransportNode> nodes = new List<TransportNode>();
        
        public RoadData(GameObject obj, Vector3 start, Vector3 end, Vector3 dir, float len, float w)
        {
            roadObject = obj;
            startPoint = start;
            endPoint = end;
            direction = dir;
            length = len;
            width = w;
        }
    }
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateFromScene();
        }
    }
    
    /// <summary>
    /// Generate network from Unity scene objects
    /// </summary>
    public void GenerateFromScene()
    {
        if (network == null)
        {
            Debug.LogError("No TransportNetwork assigned to NetworkGenerator");
            return;
        }
        
        Debug.Log("Generating transport network from scene...");
        
        // Clear existing network if requested
        if (clearBeforeGeneration)
        {
            network.Clear();
        }
        
        roadDataList.Clear();
        
        // Find all road objects and collect their data
        CollectRoadData();
        
        // Generate basic road network
        GenerateRoadNetwork();
        
        // Find and create intersection points where roads cross
        if (findCrossingIntersections)
        {
            FindCrossingIntersections();
        }
        
        // Connect intersection nodes
        if (connectIntersections)
        {
            ConnectAllIntersections();
        }
        
        // Log network statistics
        network.LogNetworkStats();
    }
    
    /// <summary>
    /// Collect data about all roads in the scene
    /// </summary>
    private void CollectRoadData()
    {
        GameObject[] roadObjects = FindObjectsOfType<GameObject>();
        List<GameObject> roads = new List<GameObject>();
        
        foreach (var obj in roadObjects)
        {
            if (((1 << obj.layer) & roadLayerMask) != 0)
            {
                roads.Add(obj);
            }
        }
        
        Debug.Log($"Found {roads.Count} road objects");
        
        // Process each road to collect data
        foreach (var roadObj in roads)
        {
            Vector3 roadDirection = GetRoadDirection(roadObj.transform);
            float roadLength = GetRoadLength(roadObj.transform, roadDirection);
            float roadWidth = GetRoadWidth(roadObj.transform, roadDirection);
            
            // Calculate start and end points
            Vector3 roadCenter = roadObj.transform.position;
            Vector3 startPos = roadCenter - roadDirection * (roadLength / 2);
            Vector3 endPos = roadCenter + roadDirection * (roadLength / 2);
            
            // Create road data
            RoadData roadData = new RoadData(
                roadObj, 
                startPos, 
                endPos, 
                roadDirection, 
                roadLength, 
                roadWidth
            );
            
            roadDataList.Add(roadData);
            
            if (debugIntersectionDetection)
            {
                Debug.DrawLine(startPos, endPos, Color.cyan, 10f);
                Debug.DrawRay(roadCenter, roadDirection * 2f, Color.magenta, 10f);
            }
        }
    }
    
    /// <summary>
    /// Generate network from road objects in scene
    /// </summary>
    private void GenerateRoadNetwork()
    {
        foreach (var roadData in roadDataList)
        {
            ProcessRoad(roadData);
        }
    }
    
    /// <summary>
    /// Process a single road object
    /// </summary>
    private void ProcessRoad(RoadData roadData)
    {
        // Calculate how many nodes we need
        int nodeCount = Mathf.Max(2, Mathf.CeilToInt(roadData.length / nodeSpacing));
        
        // Create nodes along the road
        for (int i = 0; i < nodeCount; i++)
        {
            // Calculate position (evenly spaced)
            float t = (float)i / (nodeCount - 1);
            Vector3 nodePos = Vector3.Lerp(roadData.startPoint, roadData.endPoint, t);
            
            // Determine node type
            NodeType nodeType = NodeType.RoadPoint;
            if (i == 0 || i == nodeCount - 1)
            {
                // First and last nodes are entry points
                nodeType = NodeType.EntryPoint;
            }
            
            // Create node
            string nodeId = $"{roadData.roadObject.name}_node_{i}";
            TransportNode node = network.AddNode(nodeId, nodePos, nodeType);
            
            // Add allowed modes
            node.AddAllowedMode(TransportModeType.Driving);
            
            // Add to list
            roadData.nodes.Add(node);
        }
        
        // Connect nodes with edges to form the road
        for (int i = 0; i < roadData.nodes.Count - 1; i++)
        {
            string edgeId = $"{roadData.roadObject.name}_edge_{i}";
            TransportEdge edge = network.AddEdge(edgeId, roadData.nodes[i], roadData.nodes[i + 1], EdgeType.Road);
        }
    }
    
    /// <summary>
    /// Find intersections where roads cross each other physically
    /// </summary>
    private void FindCrossingIntersections()
    {
        Debug.Log("Finding crossing intersections...");
        int intersectionsFound = 0;
        
        // Check each pair of roads for intersections
        for (int i = 0; i < roadDataList.Count; i++)
        {
            for (int j = i + 1; j < roadDataList.Count; j++)
            {
                RoadData roadA = roadDataList[i];
                RoadData roadB = roadDataList[j];
                
                // Find if these roads intersect
                Vector3 intersection;
                bool intersects = FindRoadIntersection(roadA, roadB, out intersection);
                
                if (intersects)
                {
                    // Create an intersection node
                    CreateIntersectionNode(roadA, roadB, intersection);
                    intersectionsFound++;
                }
            }
        }
        
        Debug.Log($"Found {intersectionsFound} crossing intersections");
    }
    
    /// <summary>
    /// Find if two roads intersect and where
    /// </summary>
    private bool FindRoadIntersection(RoadData roadA, RoadData roadB, out Vector3 intersection)
    {
        // Default output value
        intersection = Vector3.zero;
        
        // Get line segment representations
        Vector3 p1 = roadA.startPoint;
        Vector3 p2 = roadA.endPoint;
        Vector3 p3 = roadB.startPoint;
        Vector3 p4 = roadB.endPoint;
        
        // Working in XZ plane, ignoring Y
        float p1x = p1.x, p1z = p1.z;
        float p2x = p2.x, p2z = p2.z;
        float p3x = p3.x, p3z = p3.z;
        float p4x = p4.x, p4z = p4.z;
        
        // Line segment intersection formula
        float denominator = (p4z - p3z) * (p2x - p1x) - (p4x - p3x) * (p2z - p1z);
        
        // If lines are parallel
        if (Mathf.Abs(denominator) < 0.0001f)
        {
            return false;
        }
        
        float ua = ((p4x - p3x) * (p1z - p3z) - (p4z - p3z) * (p1x - p3x)) / denominator;
        float ub = ((p2x - p1x) * (p1z - p3z) - (p2z - p1z) * (p1x - p3x)) / denominator;
        
        // If intersection is within both line segments
        if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
        {
            // Calculate intersection point
            float intersectionX = p1x + ua * (p2x - p1x);
            float intersectionZ = p1z + ua * (p2z - p1z);
            
            // Average the Y values
            float intersectionY = (p1.y + p2.y + p3.y + p4.y) / 4f;
            
            intersection = new Vector3(intersectionX, intersectionY, intersectionZ);
            
            // Check if intersection is within width of both roads
            float distToRoadA = GetDistanceToRoadLine(intersection, p1, p2);
            float distToRoadB = GetDistanceToRoadLine(intersection, p3, p4);
            
            if (distToRoadA <= roadA.width/2 && distToRoadB <= roadB.width/2)
            {
                if (debugIntersectionDetection)
                {
                    Debug.DrawRay(intersection, Vector3.up * 3f, Color.red, 10f);
                    Debug.Log($"Intersection found at {intersection}");
                }
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Create an intersection node and connect it to the roads
    /// </summary>
    private void CreateIntersectionNode(RoadData roadA, RoadData roadB, Vector3 position)
    {
        // Create the intersection node
        string nodeId = $"intersection_{roadA.roadObject.name}_{roadB.roadObject.name}";
        TransportNode intersectionNode = network.AddNode(nodeId, position, NodeType.Intersection);
        intersectionNode.AddAllowedMode(TransportModeType.Driving);
        
        // Find closest nodes on road A
        TransportNode closestNodeA = FindClosestNodeOnRoad(roadA, position);
        
        // Find closest nodes on road B
        TransportNode closestNodeB = FindClosestNodeOnRoad(roadB, position);
        
        // Insert the intersection node into road A
        InsertNodeIntoRoad(roadA, intersectionNode, closestNodeA);
        
        // Insert the intersection node into road B
        InsertNodeIntoRoad(roadB, intersectionNode, closestNodeB);
    }
    
    /// <summary>
    /// Find the closest node on a road to a position
    /// </summary>
    private TransportNode FindClosestNodeOnRoad(RoadData road, Vector3 position)
    {
        TransportNode closestNode = null;
        float minDistance = float.MaxValue;
        
        foreach (var node in road.nodes)
        {
            float distance = Vector3.Distance(node.Position, position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = node;
            }
        }
        
        return closestNode;
    }
    
    /// <summary>
    /// Insert a node into a road's network
    /// </summary>
    private void InsertNodeIntoRoad(RoadData road, TransportNode newNode, TransportNode closestNode)
    {
        // Find the segment where the new node should be inserted
        int nodeIndex = road.nodes.IndexOf(closestNode);
        if (nodeIndex == -1)
        {
            Debug.LogError("Closest node not found in road nodes");
            return;
        }
        
        // Check if we're close to an existing node
        float distToClosest = Vector3.Distance(newNode.Position, closestNode.Position);
        if (distToClosest < 1.0f)
        {
            // Just use the existing node as an intersection
            closestNode.Type = NodeType.Intersection;
            return;
        }
        
        // Find which segment to insert into (before or after closest node)
        bool insertAfter = nodeIndex < road.nodes.Count - 1 && 
            Vector3.Dot(road.direction, newNode.Position - closestNode.Position) > 0;
        
        if (insertAfter)
        {
            if (nodeIndex + 1 < road.nodes.Count)
            {
                TransportNode nextNode = road.nodes[nodeIndex + 1];
                
                // Remove direct connection between nodes
                TransportEdge oldEdge = closestNode.GetEdgeTo(nextNode);
                if (oldEdge != null)
                {
                    network.RemoveEdge(oldEdge.ID);
                }
                
                // Add new connections
                string edgeId1 = $"{road.roadObject.name}_edge_{nodeIndex}_intersection";
                string edgeId2 = $"intersection_{road.roadObject.name}_edge_{nodeIndex+1}";
                
                network.AddEdge(edgeId1, closestNode, newNode, EdgeType.Road);
                network.AddEdge(edgeId2, newNode, nextNode, EdgeType.Road);
                
                // Insert into road's node list
                road.nodes.Insert(nodeIndex + 1, newNode);
            }
        }
        else
        {
            if (nodeIndex > 0)
            {
                TransportNode prevNode = road.nodes[nodeIndex - 1];
                
                // Remove direct connection between nodes
                TransportEdge oldEdge = prevNode.GetEdgeTo(closestNode);
                if (oldEdge != null)
                {
                    network.RemoveEdge(oldEdge.ID);
                }
                
                // Add new connections
                string edgeId1 = $"{road.roadObject.name}_edge_{nodeIndex-1}_intersection";
                string edgeId2 = $"intersection_{road.roadObject.name}_edge_{nodeIndex}";
                
                network.AddEdge(edgeId1, prevNode, newNode, EdgeType.Road);
                network.AddEdge(edgeId2, newNode, closestNode, EdgeType.Road);
                
                // Insert into road's node list
                road.nodes.Insert(nodeIndex, newNode);
            }
        }
    }
    
    /// <summary>
    /// Calculate perpendicular distance from point to line segment
    /// </summary>
    private float GetDistanceToRoadLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 line = lineEnd - lineStart;
        Vector3 pointVector = point - lineStart;
        
        // Project point onto line
        float lineLength = line.magnitude;
        if (lineLength < 0.0001f) return pointVector.magnitude;
        
        Vector3 lineDir = line / lineLength;
        float projection = Vector3.Dot(pointVector, lineDir);
        
        if (projection < 0)
            return Vector3.Distance(point, lineStart);
        else if (projection > lineLength)
            return Vector3.Distance(point, lineEnd);
        else
        {
            Vector3 projectedPoint = lineStart + lineDir * projection;
            return Vector3.Distance(point, projectedPoint);
        }
    }
    
    /// <summary>
    /// Connect all intersection nodes to nearby nodes
    /// </summary>
    private void ConnectAllIntersections()
    {
        Debug.Log("Connecting all intersections...");
        
        // Get all nodes from our network reference instead of FindObjectsOfType
        List<TransportNode> allNodes = new List<TransportNode>();
        
        // Collect all nodes from all roads
        foreach (var roadData in roadDataList)
        {
            allNodes.AddRange(roadData.nodes);
        }
        
        int connectionsAdded = 0;
        
        // Connect nodes that are close to each other
        foreach (var node in allNodes)
        {
            foreach (var otherNode in allNodes)
            {
                // Skip same node
                if (node == otherNode)
                    continue;
                    
                // Check if already connected
                if (node.IsConnectedTo(otherNode))
                    continue;
                    
                // Check distance
                float distance = Vector3.Distance(node.Position, otherNode.Position);
                
                // Draw debug lines
                if (debugIntersectionDetection && distance <= connectionThreshold * 1.5f)
                {
                    Debug.DrawLine(node.Position, otherNode.Position, 
                                Color.yellow, 10.0f);
                    Debug.Log($"Distance between nodes: {distance}");
                }
                
                if (distance <= connectionThreshold)
                {
                    // Create an edge between these nodes
                    string edgeId = $"connection_{node.ID}_{otherNode.ID}";
                    TransportEdge edge = network.AddEdge(edgeId, node, otherNode, EdgeType.Road);
                    
                    // Update node types to intersections
                    if ((node.Type == NodeType.EntryPoint || node.Type == NodeType.RoadPoint) &&
                        (otherNode.Type == NodeType.EntryPoint || otherNode.Type == NodeType.RoadPoint))
                    {
                        node.Type = NodeType.Intersection;
                        otherNode.Type = NodeType.Intersection;
                    }
                    
                    connectionsAdded++;
                }
            }
        }
        
        Debug.Log($"Connected {connectionsAdded} intersections");
    }
    /// <summary>
    /// Get the direction of a road based on its transform
    /// </summary>
    private Vector3 GetRoadDirection(Transform roadTransform)
    {
        // Get road scale to determine primary axis
        Vector3 scale = roadTransform.localScale;
        
        // First check based on scale
        if (scale.x > scale.z * 1.5f)
        {
            // X-axis is longer
            return roadTransform.right;
        }
        else if (scale.z > scale.x * 1.5f)
        {
            // Z-axis is longer
            return roadTransform.forward;
        }
        
        // If scale is similar, use rotation alignment
        float xAlignment = Mathf.Abs(Vector3.Dot(roadTransform.right, Vector3.right));
        float zAlignment = Mathf.Abs(Vector3.Dot(roadTransform.forward, Vector3.forward));
        
        return (xAlignment > zAlignment) ? roadTransform.right : roadTransform.forward;
    }
    
    /// <summary>
    /// Get the length of a road along its main axis
    /// </summary>
    private float GetRoadLength(Transform roadTransform, Vector3 direction)
    {
        // If using local X axis
        if (Vector3.Dot(direction, roadTransform.right) > 0.5f)
        {
            return roadTransform.localScale.x;
        }
        // If using local Z axis
        else
        {
            return roadTransform.localScale.z;
        }
    }
    
    /// <summary>
    /// Get the width of a road perpendicular to its main axis
    /// </summary>
    private float GetRoadWidth(Transform roadTransform, Vector3 direction)
    {
        // If using local X axis
        if (Vector3.Dot(direction, roadTransform.right) > 0.5f)
        {
            return roadTransform.localScale.z;
        }
        // If using local Z axis
        else
        {
            return roadTransform.localScale.x;
        }
    }
}
