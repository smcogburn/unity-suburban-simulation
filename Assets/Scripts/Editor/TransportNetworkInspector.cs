using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UrbanSim.Editor
{
    [CustomEditor(typeof(TransportNetwork))]
    public class TransportNetworkInspector : UnityEditor.Editor
    {
        private bool showNodes = true;
        private bool showEdges = true;
        private bool showStats = true;
        private Vector2 nodeScrollPosition;
        private Vector2 edgeScrollPosition;
        
        public override void OnInspectorGUI()
        {
            TransportNetwork network = (TransportNetwork)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transport Network", EditorStyles.boldLabel);
            
            // Display basic statistics
            List<NodeData> nodes = network.GetAllNodes();
            List<EdgeData> edges = network.GetAllEdges();
            
            EditorGUILayout.LabelField($"Nodes: {nodes.Count}", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Edges: {edges.Count}", EditorStyles.miniBoldLabel);
            
            // Stats section
            showStats = EditorGUILayout.Foldout(showStats, "Network Statistics", true);
            if (showStats)
            {
                float totalLength = 0f;
                float walkableLength = 0f;
                float drivableLength = 0f;
                int disconnectedNodes = 0;
                
                // Calculate stats
                foreach (EdgeData edge in edges)
                {
                    edge.CalculateLength(network);
                    totalLength += edge.length;
                    
                    if ((edge.allowedModes & TransportMode.Walking) != 0)
                        walkableLength += edge.length;
                        
                    if ((edge.allowedModes & TransportMode.Driving) != 0)
                        drivableLength += edge.length;
                }
                
                // Count disconnected nodes
                foreach (NodeData node in nodes)
                {
                    if (network.GetEdgesForNode(node.id).Count == 0)
                        disconnectedNodes++;
                }
                
                EditorGUILayout.LabelField($"Total network length: {totalLength:F2} units");
                
                if (totalLength > 0)
                {
                    EditorGUILayout.LabelField($"Walkable network: {walkableLength:F2} units ({walkableLength / totalLength * 100:F1}%)");
                    EditorGUILayout.LabelField($"Drivable network: {drivableLength:F2} units ({drivableLength / totalLength * 100:F1}%)");
                }
                
                if (disconnectedNodes > 0)
                {
                    EditorGUILayout.HelpBox($"Found {disconnectedNodes} disconnected nodes!", MessageType.Warning);
                }
            }
            
            // Node list
            showNodes = EditorGUILayout.Foldout(showNodes, "Nodes", true);
            if (showNodes)
            {
                nodeScrollPosition = EditorGUILayout.BeginScrollView(nodeScrollPosition, 
                    GUILayout.Height(150));
                
                foreach (NodeData node in nodes)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Node info
                    EditorGUILayout.LabelField(node.displayName, GUILayout.Width(80));
                    
                    // Position field
                    Vector3 newPosition = EditorGUILayout.Vector3Field("", node.position);
                    if (newPosition != node.position)
                    {
                        Undo.RecordObject(network, "Move Node");
                        node.position = newPosition;
                        network.UpdateEdgeLengths();
                        EditorUtility.SetDirty(network);
                    }
                    
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Node", 
                            "Are you sure you want to remove this node? All connected edges will also be removed.", 
                            "Yes", "No"))
                        {
                            Undo.RecordObject(network, "Remove Node");
                            network.RemoveNode(node.id);
                            EditorUtility.SetDirty(network);
                            break;
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            // Edge list
            showEdges = EditorGUILayout.Foldout(showEdges, "Edges", true);
            if (showEdges)
            {
                edgeScrollPosition = EditorGUILayout.BeginScrollView(edgeScrollPosition, 
                    GUILayout.Height(150));
                
                foreach (EdgeData edge in edges)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Edge info
                    NodeData startNode = network.GetNodeById(edge.startNodeId);
                    NodeData endNode = network.GetNodeById(edge.endNodeId);
                    
                    string startName = startNode?.displayName ?? "Missing";
                    string endName = endNode?.displayName ?? "Missing";
                    
                    EditorGUILayout.LabelField($"{startName} → {endName}", GUILayout.Width(150));
                    
                    // Transport modes
                    TransportMode newModes = (TransportMode)EditorGUILayout.EnumFlagsField(
                        "", edge.allowedModes, GUILayout.Width(100));
                    
                    if (newModes != edge.allowedModes)
                    {
                        Undo.RecordObject(network, "Change Transport Modes");
                        edge.allowedModes = newModes;
                        EditorUtility.SetDirty(network);
                    }
                    
                    // One-way toggle
                    bool newOneWay = EditorGUILayout.Toggle(edge.isOneWay, GUILayout.Width(20));
                    if (newOneWay != edge.isOneWay)
                    {
                        Undo.RecordObject(network, "Toggle One-Way");
                        edge.isOneWay = newOneWay;
                        EditorUtility.SetDirty(network);
                    }
                    
                    // Curvature
                    float newCurvature = EditorGUILayout.Slider(edge.curvature, 0f, 5f, GUILayout.Width(100));
                    if (newCurvature != edge.curvature)
                    {
                        Undo.RecordObject(network, "Change Curvature");
                        edge.curvature = newCurvature;
                        EditorUtility.SetDirty(network);
                    }
                    
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Edge", 
                            "Are you sure you want to remove this edge?", 
                            "Yes", "No"))
                        {
                            Undo.RecordObject(network, "Remove Edge");
                            network.RemoveEdge(edge.id);
                            EditorUtility.SetDirty(network);
                            break;
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            // Add buttons for network editing
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Node"))
            {
                // Create a node at origin
                Undo.RecordObject(network, "Add Node");
                network.AddNode(Vector3.zero);
                EditorUtility.SetDirty(network);
            }
            
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear Network", 
                    "Are you sure you want to remove all nodes and edges?", 
                    "Yes", "No"))
                {
                    Undo.RecordObject(network, "Clear Network");
                    List<NodeData> allNodes = new List<NodeData>(network.GetAllNodes());
                    foreach (NodeData node in allNodes)
                    {
                        network.RemoveNode(node.id);
                    }
                    EditorUtility.SetDirty(network);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Help message
            EditorGUILayout.HelpBox(
                "For more advanced network editing, use the Network Editor Window (Urban Simulation > Network Editor)", 
                MessageType.Info);
        }
    }
}