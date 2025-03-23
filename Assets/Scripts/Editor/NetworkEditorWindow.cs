using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UrbanSim.Editor
{
    public class NetworkEditorWindow : EditorWindow
    {
        private TransportNetwork currentNetwork;
        private NodeData selectedNode;
        private EdgeData selectedEdge;
        private bool isCreatingEdge = false;
        private NodeData edgeStartNode;
        
        // Tool modes
        private enum EditMode
        {
            Select,
            CreateNode,
            CreateEdge
        }
        
        private EditMode currentMode = EditMode.Select;
        private Color[] modeColors = new Color[] 
        {
            new Color(0.3f, 0.7f, 0.3f),  // Select
            new Color(0.2f, 0.6f, 1.0f),  // Create Node
            new Color(1.0f, 0.6f, 0.2f)   // Create Edge
        };
        
        // Settings
        private float nodeRadius = 1f;
        private float edgeWidth = 0.3f;
        private float gridSize = 1f;
        private bool snapToGrid = true;
        private bool showFloatingControls = true;
        
        [MenuItem("Urban Simulation/Network Editor")]
        public static void ShowWindow()
        {
            GetWindow<NetworkEditorWindow>("Transport Network Editor");
        }
        
        private void OnEnable()
        {
            // Register for SceneView updates
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            
            // Set initial tool
            Tools.current = Tool.None;
        }
        
        private void OnDisable()
        {
            // Unregister SceneView updates
            SceneView.duringSceneGui -= OnSceneGUI;
            
            // Restore normal Unity tools
            Tools.current = Tool.Move;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label("Transport Network Editor", EditorStyles.boldLabel);
            
            // Network selection
            EditorGUILayout.BeginHorizontal();
            TransportNetwork newNetwork = (TransportNetwork)EditorGUILayout.ObjectField(
                "Network Asset", currentNetwork, typeof(TransportNetwork), false);
                
            if (newNetwork != currentNetwork)
            {
                // Network changed
                currentNetwork = newNetwork;
                selectedNode = null;
                selectedEdge = null;
                isCreatingEdge = false;
                edgeStartNode = null;
                
                if (currentNetwork != null)
                {
                    // Focus on network in scene
                    if (currentNetwork.GetAllNodes().Count > 0)
                    {
                        Vector3 centerPos = Vector3.zero;
                        foreach (var node in currentNetwork.GetAllNodes())
                        {
                            centerPos += node.position;
                        }
                        centerPos /= currentNetwork.GetAllNodes().Count;
                        
                        SceneView.lastActiveSceneView.LookAt(centerPos);
                    }
                }
            }
            
            if (GUILayout.Button("Create New", GUILayout.Width(100)))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Transport Network", "TransportNetwork", "asset", 
                    "Choose a location to save the new transport network");
                    
                if (!string.IsNullOrEmpty(path))
                {
                    TransportNetwork newNetworkAsset = CreateInstance<TransportNetwork>();
                    AssetDatabase.CreateAsset(newNetworkAsset, path);
                    AssetDatabase.SaveAssets();
                    currentNetwork = newNetworkAsset;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (currentNetwork == null)
            {
                EditorGUILayout.HelpBox("Please select or create a Transport Network asset.", MessageType.Info);
                return;
            }
            
            // Tool selection
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Edit Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = modeColors[0];
            if (GUILayout.Toggle(currentMode == EditMode.Select, "Select", EditorStyles.miniButtonLeft))
            {
                currentMode = EditMode.Select;
            }
            
            GUI.backgroundColor = modeColors[1];
            if (GUILayout.Toggle(currentMode == EditMode.CreateNode, "Create Node", EditorStyles.miniButtonMid))
            {
                currentMode = EditMode.CreateNode;
            }
            
            GUI.backgroundColor = modeColors[2];
            if (GUILayout.Toggle(currentMode == EditMode.CreateEdge, "Create Edge", EditorStyles.miniButtonRight))
            {
                currentMode = EditMode.CreateEdge;
                if (selectedNode == null)
                {
                    EditorGUILayout.HelpBox("Select a node first to start creating an edge.", MessageType.Info);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            // Settings
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            
            nodeRadius = EditorGUILayout.Slider("Node Size", nodeRadius, 0.2f, 3f);
            edgeWidth = EditorGUILayout.Slider("Edge Width", edgeWidth, 0.1f, 1f);
            
            EditorGUILayout.BeginHorizontal();
            snapToGrid = EditorGUILayout.Toggle("Snap to Grid", snapToGrid);
            if (snapToGrid)
            {
                gridSize = EditorGUILayout.FloatField(gridSize, GUILayout.Width(50));
            }
            EditorGUILayout.EndHorizontal();
            
            showFloatingControls = EditorGUILayout.Toggle("Show Floating Controls", showFloatingControls);
            
            // Information about selected items
            EditorGUILayout.Space();
            if (selectedNode != null)
            {
                EditorGUILayout.LabelField("Selected Node", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("ID: " + selectedNode.id);
                
                // Allow editing node position
                Vector3 newPos = EditorGUILayout.Vector3Field("Position", selectedNode.position);
                if (newPos != selectedNode.position)
                {
                    Undo.RecordObject(currentNetwork, "Move Node");
                    selectedNode.position = newPos;
                    currentNetwork.UpdateEdgeLengths();
                    EditorUtility.SetDirty(currentNetwork);
                }
                
                // Show connected edges
                List<EdgeData> connectedEdges = currentNetwork.GetEdgesForNode(selectedNode.id);
                EditorGUILayout.LabelField($"Connected Edges: {connectedEdges.Count}");
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Delete Node"))
                {
                    if (EditorUtility.DisplayDialog("Delete Node", 
                        "Are you sure you want to delete this node? All connected edges will also be removed.", 
                        "Yes", "No"))
                    {
                        Undo.RecordObject(currentNetwork, "Delete Node");
                        currentNetwork.RemoveNode(selectedNode.id);
                        selectedNode = null;
                        EditorUtility.SetDirty(currentNetwork);
                    }
                }
                
                if (currentMode == EditMode.CreateEdge && !isCreatingEdge)
                {
                    if (GUILayout.Button("Start Edge"))
                    {
                        isCreatingEdge = true;
                        edgeStartNode = selectedNode;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (selectedEdge != null)
            {
                EditorGUILayout.LabelField("Selected Edge", EditorStyles.boldLabel);
                
                NodeData startNode = currentNetwork.GetNodeById(selectedEdge.startNodeId);
                NodeData endNode = currentNetwork.GetNodeById(selectedEdge.endNodeId);
                
                string startName = startNode?.displayName ?? "Missing";
                string endName = endNode?.displayName ?? "Missing";
                
                EditorGUILayout.LabelField($"Connection: {startName} → {endName}");
                
                // Edit edge properties
                EditorGUI.BeginChangeCheck();
                TransportMode newModes = (TransportMode)EditorGUILayout.EnumFlagsField(
                    "Transport Modes", selectedEdge.allowedModes);
                    
                bool newOneWay = EditorGUILayout.Toggle("One Way", selectedEdge.isOneWay);
                
                float newCurvature = EditorGUILayout.Slider("Curvature", selectedEdge.curvature, 0f, 5f);
                
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(currentNetwork, "Edit Edge");
                    selectedEdge.allowedModes = newModes;
                    selectedEdge.isOneWay = newOneWay;
                    selectedEdge.curvature = newCurvature;
                    EditorUtility.SetDirty(currentNetwork);
                }
                
                if (GUILayout.Button("Delete Edge"))
                {
                    if (EditorUtility.DisplayDialog("Delete Edge", 
                        "Are you sure you want to delete this edge?", 
                        "Yes", "No"))
                    {
                        Undo.RecordObject(currentNetwork, "Delete Edge");
                        currentNetwork.RemoveEdge(selectedEdge.id);
                        selectedEdge = null;
                        EditorUtility.SetDirty(currentNetwork);
                    }
                }
            }
            
            // Status information while creating an edge
            if (isCreatingEdge && edgeStartNode != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    $"Creating edge from {edgeStartNode.displayName}. Select another node to complete or press Escape to cancel.",
                    MessageType.Info);
            }
            
            // Buttons for network operations
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Grid"))
            {
                CreateGridNetwork(5, 5, 10f);
            }
            
            if (GUILayout.Button("Select All"))
            {
                // Currently just focuses on the network
                if (currentNetwork.GetAllNodes().Count > 0)
                {
                    Vector3 centerPos = Vector3.zero;
                    foreach (var node in currentNetwork.GetAllNodes())
                    {
                        centerPos += node.position;
                    }
                    centerPos /= currentNetwork.GetAllNodes().Count;
                    
                    SceneView.lastActiveSceneView.LookAt(centerPos);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Instructions
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Left-click in scene to place nodes or select items.\n" +
                "Hold Shift while clicking to add multiple nodes.\n" +
                "Press Delete to remove selected node or edge.\n" +
                "Press Escape to cancel edge creation.",
                MessageType.Info);
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (currentNetwork == null)
                return;
                
            Event e = Event.current;
            
            // Set cursor based on mode
            if (e.type == EventType.Layout)
            {
                switch (currentMode)
                {
                    case EditMode.Select:
                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                        break;
                    case EditMode.CreateNode:
                        EditorGUIUtility.AddCursorRect(new Rect(0, 0, sceneView.position.width, sceneView.position.height), MouseCursor.Arrow);
                        break;
                    case EditMode.CreateEdge:
                        EditorGUIUtility.AddCursorRect(new Rect(0, 0, sceneView.position.width, sceneView.position.height), MouseCursor.ArrowPlus);
                        break;
                }
            }
            
            // Handle key events
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    if (isCreatingEdge)
                    {
                        isCreatingEdge = false;
                        edgeStartNode = null;
                        Repaint();
                        e.Use();
                    }
                }
                else if (e.keyCode == KeyCode.Delete)
                {
                    if (selectedNode != null)
                    {
                        Undo.RecordObject(currentNetwork, "Delete Node");
                        currentNetwork.RemoveNode(selectedNode.id);
                        selectedNode = null;
                        EditorUtility.SetDirty(currentNetwork);
                        Repaint();
                        e.Use();
                    }
                    else if (selectedEdge != null)
                    {
                        Undo.RecordObject(currentNetwork, "Delete Edge");
                        currentNetwork.RemoveEdge(selectedEdge.id);
                        selectedEdge = null;
                        EditorUtility.SetDirty(currentNetwork);
                        Repaint();
                        e.Use();
                    }
                }
            }
            
            // Handle mouse events
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;
                
                // Check for node/edge selection first
                bool hitSomething = false;
                float pickDistance = HandleUtility.GetHandleSize(Vector3.zero) * 0.2f;
                
                // First try to pick a node
                foreach (NodeData node in currentNetwork.GetAllNodes())
                {
                    float distToRay = HandleUtility.DistancePointLine(node.position, ray.origin, ray.origin + ray.direction * 100f);
                    if (distToRay < nodeRadius)
                    {
                        // Hit a node
                        hitSomething = true;
                        selectedNode = node;
                        selectedEdge = null;
                        
                        if (isCreatingEdge && edgeStartNode != null && selectedNode != edgeStartNode)
                        {
                            // Complete edge creation
                            Undo.RecordObject(currentNetwork, "Create Edge");
                            currentNetwork.AddEdge(edgeStartNode.id, selectedNode.id);
                            isCreatingEdge = false;
                            edgeStartNode = null;
                            EditorUtility.SetDirty(currentNetwork);
                        }
                        
                        Repaint();
                        e.Use();
                        break;
                    }
                }
                
                // If no node was hit, try edges
                if (!hitSomething)
                {
                    foreach (EdgeData edge in currentNetwork.GetAllEdges())
                    {
                        NodeData startNode = currentNetwork.GetNodeById(edge.startNodeId);
                        NodeData endNode = currentNetwork.GetNodeById(edge.endNodeId);
                        
                        if (startNode != null && endNode != null)
                        {
                            float distToRay;
                            
                            if (edge.curvature > 0)
                            {
                                // For curved edges, check a few points along the curve
                                float minDist = float.MaxValue;
                                int samples = 10;
                                
                                // Calculate control point
                                Vector3 midPoint = (startNode.position + endNode.position) / 2f;
                                Vector3 direction = (endNode.position - startNode.position).normalized;
                                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                                Vector3 controlPoint = midPoint + perpendicular * edge.curvature;
                                
                                // Check samples
                                for (int i = 0; i <= samples; i++)
                                {
                                    float t = i / (float)samples;
                                    Vector3 point = QuadraticBezier(startNode.position, controlPoint, endNode.position, t);
                                    float dist = HandleUtility.DistancePointLine(point, ray.origin, ray.origin + ray.direction * 100f);
                                    if (dist < minDist)
                                        minDist = dist;
                                }
                                
                                distToRay = minDist;
                            }
                            else
                            {
                                // For straight edges, just check the line
                                distToRay = HandleUtility.DistanceToLine(startNode.position, endNode.position);
                            }
                            
                            if (distToRay < edgeWidth + 0.1f)
                            {
                                // Hit an edge
                                hitSomething = true;
                                selectedEdge = edge;
                                selectedNode = null;
                                isCreatingEdge = false;
                                edgeStartNode = null;
                                Repaint();
                                e.Use();
                                break;
                            }
                        }
                    }
                }
                
                // If no node or edge was hit, and we're in create node mode, create a new node
                if (!hitSomething && currentMode == EditMode.CreateNode)
                {
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    float distance;
                    
                    if (groundPlane.Raycast(ray, out distance))
                    {
                        Vector3 hitPoint = ray.GetPoint(distance);
                        
                        // Snap to grid if enabled
                        if (snapToGrid)
                        {
                            hitPoint.x = Mathf.Round(hitPoint.x / gridSize) * gridSize;
                            hitPoint.z = Mathf.Round(hitPoint.z / gridSize) * gridSize;
                        }
                        
                        Undo.RecordObject(currentNetwork, "Create Node");
                        NodeData newNode = currentNetwork.AddNode(hitPoint);
                        selectedNode = newNode;
                        selectedEdge = null;
                        
                        // If we're creating an edge and shift is held, create a connection
                        if (isCreatingEdge && edgeStartNode != null && e.shift)
                        {
                            currentNetwork.AddEdge(edgeStartNode.id, newNode.id);
                            // Keep creating edges if shift is held
                            edgeStartNode = newNode;
                        }
                        else if (isCreatingEdge && edgeStartNode != null)
                        {
                            // Complete edge creation
                            currentNetwork.AddEdge(edgeStartNode.id, newNode.id);
                            isCreatingEdge = false;
                            edgeStartNode = null;
                        }
                        
                        EditorUtility.SetDirty(currentNetwork);
                        e.Use();
                        Repaint();
                    }
                }
            }
            
            // Draw handles for nodes
            foreach (NodeData node in currentNetwork.GetAllNodes())
            {
                // Determine node color based on selection and connections
                Color nodeColor = Color.white;
                float nodeSizeMultiplier = 1.0f;
                
                if (node == selectedNode)
                {
                    nodeColor = Color.yellow; // Selected
                    nodeSizeMultiplier = 1.2f;
                }
                else if (node == edgeStartNode)
                {
                    nodeColor = new Color(1.0f, 0.6f, 0.2f); // Edge start
                    nodeSizeMultiplier = 1.2f;
                }
                else
                {
                    // Check connections
                    List<EdgeData> connectedEdges = currentNetwork.GetEdgesForNode(node.id);
                    if (connectedEdges.Count == 0)
                    {
                        nodeColor = Color.red; // Disconnected
                    }
                    else
                    {
                        bool hasWalking = false;
                        bool hasDriving = false;
                        
                        foreach (var edge in connectedEdges)
                        {
                            if ((edge.allowedModes & TransportMode.Walking) != 0)
                                hasWalking = true;
                            
                            if ((edge.allowedModes & TransportMode.Driving) != 0)
                                hasDriving = true;
                        }
                        
                        if (hasWalking && hasDriving)
                        {
                            nodeColor = new Color(0.6f, 0.3f, 0.8f); // Purple (mixed)
                        }
                        else if (hasWalking)
                        {
                            nodeColor = new Color(0.3f, 0.7f, 1.0f); // Blue (walking)
                        }
                        else if (hasDriving)
                        {
                            nodeColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray (driving)
                        }
                    }
                }
                
                Handles.color = nodeColor;
                
                // Draw node
                float handleSize = nodeRadius * nodeSizeMultiplier;
                if (Handles.Button(node.position, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
                {
                    // Handle selection
                    selectedNode = node;
                    selectedEdge = null;
                    
                    if (isCreatingEdge && edgeStartNode != null && selectedNode != edgeStartNode)
                    {
                        // Complete edge creation
                        Undo.RecordObject(currentNetwork, "Create Edge");
                        currentNetwork.AddEdge(edgeStartNode.id, selectedNode.id);
                        
                        // If shift is held, continue creating edges from the newly selected node
                        if (Event.current.shift)
                        {
                            edgeStartNode = selectedNode;
                        }
                        else
                        {
                            isCreatingEdge = false;
                            edgeStartNode = null;
                        }
                        
                        EditorUtility.SetDirty(currentNetwork);
                    }
                    
                    Repaint();
                    Event.current.Use();
                }
                
                // Allow node position to be moved if selected
                if (node == selectedNode)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.PositionHandle(node.position, Quaternion.identity);
                    
                    if (snapToGrid)
                    {
                        newPos.x = Mathf.Round(newPos.x / gridSize) * gridSize;
                        newPos.z = Mathf.Round(newPos.z / gridSize) * gridSize;
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(currentNetwork, "Move Node");
                        node.position = newPos;
                        currentNetwork.UpdateEdgeLengths();
                        EditorUtility.SetDirty(currentNetwork);
                    }
                }
                
                // Optionally draw node labels
                Handles.color = Color.white;
                Handles.Label(node.position + Vector3.up * nodeRadius * 1.5f, node.displayName);
            }
            
            // Draw edges
            foreach (EdgeData edge in currentNetwork.GetAllEdges())
            {
                NodeData startNode = currentNetwork.GetNodeById(edge.startNodeId);
                NodeData endNode = currentNetwork.GetNodeById(edge.endNodeId);
                
                if (startNode != null && endNode != null)
                {
                    // Determine edge color based on selection and transport modes
                    Color edgeColor;
                    float edgeThickness = edgeWidth;
                    
                    if (edge == selectedEdge)
                    {
                        edgeColor = Color.yellow; // Selected
                        edgeThickness = edgeWidth * 1.5f;
                    }
                    else if (edge.allowedModes == TransportMode.None)
                    {
                        edgeColor = Color.gray; // No transport modes
                        edgeThickness = edgeWidth * 0.5f;
                    }
                    else if (edge.allowedModes == TransportMode.Walking)
                    {
                        edgeColor = new Color(0.2f, 0.6f, 1.0f); // Blue (walking only)
                        edgeThickness = edgeWidth * 0.7f;
                    }
                    else if (edge.allowedModes == TransportMode.Driving)
                    {
                        edgeColor = Color.black; // Black (driving only)
                    }
                    else if (edge.allowedModes == (TransportMode.Driving | TransportMode.Walking))
                    {
                        edgeColor = new Color(0.6f, 0.2f, 0.8f); // Purple (both)
                        edgeThickness = edgeWidth * 1.2f;
                    }
                    else
                    {
                        edgeColor = Color.cyan; // Other combinations
                    }
                    
                    Handles.color = edgeColor;
                    
                    // Draw the edge
                    if (edge.curvature > 0)
                    {
                        // Calculate midpoint and offset it for curve
                        Vector3 midPoint = (startNode.position + endNode.position) / 2f;
                        Vector3 direction = (endNode.position - startNode.position).normalized;
                        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                        midPoint += perpendicular * edge.curvature;
                        
                        // Draw bezier
                        Handles.DrawBezier(
                            startNode.position,
                            endNode.position,
                            Vector3.Lerp(startNode.position, midPoint, 0.5f),
                            Vector3.Lerp(endNode.position, midPoint, 0.5f),
                            edgeColor,
                            null,
                            edgeThickness
                        );
                    }
                    else
                    {
                        // Draw straight line
                        Handles.DrawLine(startNode.position, endNode.position, edgeThickness);
                    }
                    
                    // Draw direction arrow for one-way streets
                    if (edge.isOneWay)
                    {
                        Vector3 direction = (endNode.position - startNode.position).normalized;
                        Vector3 arrowPos;
                        
                        if (edge.curvature > 0)
                        {
                            // Place arrow at 2/3 along curved path
                            Vector3 midPoint = (startNode.position + endNode.position) / 2f;
                            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                            midPoint += perpendicular * edge.curvature;
                            
                            arrowPos = QuadraticBezier(startNode.position, midPoint, endNode.position, 0.67f);
                            direction = QuadraticBezierTangent(startNode.position, midPoint, endNode.position, 0.67f).normalized;
                        }
                        else
                        {
                            // Place arrow at 2/3 along straight line
                            arrowPos = Vector3.Lerp(startNode.position, endNode.position, 0.67f);
                        }
                        
                        // Draw a small arrow
                        float arrowSize = nodeRadius * 0.8f;
                        Vector3 right = Quaternion.Euler(0, -30, 0) * -direction * arrowSize;
                        Vector3 left = Quaternion.Euler(0, 30, 0) * -direction * arrowSize;
                        
                        Handles.DrawLine(arrowPos, arrowPos + right);
                        Handles.DrawLine(arrowPos, arrowPos + left);
                    }
                }
            }
            
            // Draw preview line when creating an edge
            if (isCreatingEdge && edgeStartNode != null)
            {
                Handles.color = new Color(1.0f, 0.6f, 0.2f);
                
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                float distance;
                
                if (groundPlane.Raycast(ray, out distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    
                    // Snap preview point to grid if enabled
                    if (snapToGrid)
                    {
                        hitPoint.x = Mathf.Round(hitPoint.x / gridSize) * gridSize;
                        hitPoint.z = Mathf.Round(hitPoint.z / gridSize) * gridSize;
                    }
                    
                    Handles.DrawLine(edgeStartNode.position, hitPoint);
                    
                    // Also draw a temporary node at mouse position
                    Handles.color = new Color(1.0f, 0.6f, 0.2f, 0.5f);
                    Handles.SphereHandleCap(0, hitPoint, Quaternion.identity, nodeRadius, EventType.Repaint);
                }
            }
            
            // Draw floating controls if enabled
            if (showFloatingControls)
            {
                Handles.BeginGUI();
                
                // Draw floating mode buttons at top-left of scene view
                float buttonWidth = 80;
                float buttonHeight = 25;
                float padding = 10;
                float startX = padding;
                float startY = padding;
                
                // Use the current tool mode colors
                GUI.backgroundColor = modeColors[0];
                if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Select"))
                {
                    currentMode = EditMode.Select;
                    Repaint();
                }
                
                GUI.backgroundColor = modeColors[1];
                if (GUI.Button(new Rect(startX + buttonWidth + 5, startY, buttonWidth, buttonHeight), "Add Node"))
                {
                    currentMode = EditMode.CreateNode;
                    Repaint();
                }
                
                GUI.backgroundColor = modeColors[2];
                if (GUI.Button(new Rect(startX + (buttonWidth + 5) * 2, startY, buttonWidth, buttonHeight), "Add Edge"))
                {
                    currentMode = EditMode.CreateEdge;
                    Repaint();
                }
                
                // Reset color
                GUI.backgroundColor = Color.white;
                
                Handles.EndGUI();
            }
        }
        
        // Helper for quadratic bezier curve
        private Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            
            return uu * p0 + 2 * u * t * p1 + tt * p2;
        }
        
        // Helper for bezier tangent
        private Vector3 QuadraticBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
        }
        
        // Helper to create a grid network
        private void CreateGridNetwork(int rows, int columns, float spacing)
        {
            if (currentNetwork == null)
                return;
                
            Undo.RecordObject(currentNetwork, "Create Grid Network");
            
            // Create nodes in a grid pattern
            Dictionary<Vector2Int, NodeData> nodeGrid = new Dictionary<Vector2Int, NodeData>();
            
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    Vector3 position = new Vector3(x * spacing, 0, z * spacing);
                    NodeData node = currentNetwork.AddNode(position);
                    nodeGrid[new Vector2Int(x, z)] = node;
                }
            }
            
            // Connect nodes with edges
            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    NodeData current = nodeGrid[new Vector2Int(x, z)];
                    
                    // Connect to the node to the right
                    if (x < columns - 1)
                    {
                        NodeData right = nodeGrid[new Vector2Int(x + 1, z)];
                        currentNetwork.AddEdge(current.id, right.id);
                    }
                    
                    // Connect to the node above
                    if (z < rows - 1)
                    {
                        NodeData above = nodeGrid[new Vector2Int(x, z + 1)];
                        currentNetwork.AddEdge(current.id, above.id);
                    }
                }
            }
            
            EditorUtility.SetDirty(currentNetwork);
            
            // Focus scene view on the new grid
            Vector3 centerPos = new Vector3((columns - 1) * spacing / 2f, 0, (rows - 1) * spacing / 2f);
            float viewSize = Mathf.Max(columns, rows) * spacing;
            
            SceneView.lastActiveSceneView.LookAt(centerPos, Quaternion.Euler(30, -45, 0), viewSize * 1.5f);
        }
    }
}