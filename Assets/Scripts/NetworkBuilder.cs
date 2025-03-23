using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UrbanSim
{
    /// <summary>
    /// Utility class for creating network objects in the scene from a Transport Network asset
    /// </summary>
    public class NetworkBuilder : MonoBehaviour
    {
        public TransportNetwork networkAsset;
        public bool autoUpdateVisuals = true;
        
        // References to generated objects
        [SerializeField, HideInInspector]
        private List<GameObject> generatedObjects = new List<GameObject>();
        
        // Build a complete network visual representation
        public void BuildNetwork()
        {
            if (networkAsset == null)
            {
                Debug.LogError("Cannot build network: No network asset assigned");
                return;
            }
            
            // Clear any existing objects
            ClearNetwork();
            
            // Create container objects
            Transform nodesContainer = new GameObject("Nodes").transform;
            nodesContainer.SetParent(transform);
            
            Transform edgesContainer = new GameObject("Edges").transform;
            edgesContainer.SetParent(transform);
            
            generatedObjects.Add(nodesContainer.gameObject);
            generatedObjects.Add(edgesContainer.gameObject);
            
            // Build node presenters
            Dictionary<string, NodePresenter> nodeMap = new Dictionary<string, NodePresenter>();
            
            foreach (NodeData nodeData in networkAsset.GetAllNodes())
            {
                GameObject nodeObj = new GameObject($"Node_{nodeData.displayName}");
                nodeObj.transform.SetParent(nodesContainer);
                
                NodePresenter presenter = nodeObj.AddComponent<NodePresenter>();
                presenter.network = networkAsset;
                presenter.nodeId = nodeData.id;
                presenter.ReloadNodeData();
                
                nodeMap[nodeData.id] = presenter;
                generatedObjects.Add(nodeObj);
            }
            
            // Build edge presenters
            foreach (EdgeData edgeData in networkAsset.GetAllEdges())
            {
                GameObject edgeObj = new GameObject($"Edge_{edgeData.id.Substring(0, 5)}");
                edgeObj.transform.SetParent(edgesContainer);
                
                EdgePresenter presenter = edgeObj.AddComponent<EdgePresenter>();
                presenter.network = networkAsset;
                presenter.edgeId = edgeData.id;
                presenter.ReloadEdgeData();
                
                generatedObjects.Add(edgeObj);
            }
            
            Debug.Log($"Built network visualization with {nodeMap.Count} nodes and {networkAsset.GetAllEdges().Count} edges");
        }
        
        public void ClearNetwork()
        {
            foreach (GameObject obj in generatedObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            
            generatedObjects.Clear();
        }
        
        // Update visuals from network asset
        public void UpdateFromAsset()
        {
            if (generatedObjects.Count == 0)
            {
                BuildNetwork();
            }
            else
            {
                // Refresh existing objects
                NodePresenter[] nodePresenters = GetComponentsInChildren<NodePresenter>();
                foreach (NodePresenter presenter in nodePresenters)
                {
                    presenter.ReloadNodeData();
                }
                
                EdgePresenter[] edgePresenters = GetComponentsInChildren<EdgePresenter>();
                foreach (EdgePresenter presenter in edgePresenters)
                {
                    presenter.ReloadEdgeData();
                }
            }
        }
        
        private void OnValidate()
        {
            if (autoUpdateVisuals && networkAsset != null)
            {
                UpdateFromAsset();
            }
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(NetworkBuilder))]
    public class NetworkBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            NetworkBuilder builder = (NetworkBuilder)target;
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Build Network"))
            {
                builder.BuildNetwork();
            }
            
            if (GUILayout.Button("Clear Network"))
            {
                builder.ClearNetwork();
            }
            
            if (GUILayout.Button("Update From Asset"))
            {
                builder.UpdateFromAsset();
            }
            
            EditorGUILayout.HelpBox(
                "The NetworkBuilder creates visual representations of nodes and edges from a TransportNetwork asset.\n\n" +
                "For more advanced network editing, use the Network Editor Window (Urban Simulation > Network Editor).",
                MessageType.Info);
        }
    }
    #endif
}