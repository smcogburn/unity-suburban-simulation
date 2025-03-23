using System.Collections.Generic;
using UnityEngine;

namespace UrbanSim
{
    public class TransportNetworkManager : MonoBehaviour
    {
        public TransportNetwork networkAsset;
        public bool generateVisualsOnStart = true;
        
        private Dictionary<string, NodePresenter> nodePresenterMap = new Dictionary<string, NodePresenter>();
        private Dictionary<string, EdgePresenter> edgePresenterMap = new Dictionary<string, EdgePresenter>();
        
        // Container objects for organization
        private Transform nodesContainer;
        private Transform edgesContainer;
        
        private void Awake()
        {
            if (networkAsset == null)
            {
                Debug.LogError("TransportNetworkManager requires a NetworkAsset");
                return;
            }
            
            // Create container objects
            nodesContainer = new GameObject("Nodes").transform;
            nodesContainer.SetParent(transform);
            
            edgesContainer = new GameObject("Edges").transform;
            edgesContainer.SetParent(transform);
            
            if (generateVisualsOnStart)
            {
                LoadNetwork();
            }
        }
        
        public void LoadNetwork()
        {
            ClearCurrentNetwork();
            
            // Create all node presenters
            foreach (NodeData nodeData in networkAsset.GetAllNodes())
            {
                CreateNodePresenter(nodeData);
            }
            
            // Create all edge presenters
            foreach (EdgeData edgeData in networkAsset.GetAllEdges())
            {
                CreateEdgePresenter(edgeData);
            }
            
            Debug.Log($"Loaded transport network with {nodePresenterMap.Count} nodes and {edgePresenterMap.Count} edges");
        }
        
        private void ClearCurrentNetwork()
        {
            // Destroy all node presenters
            foreach (var presenter in nodePresenterMap.Values)
            {
                if (presenter != null)
                {
                    DestroyImmediate(presenter.gameObject);
                }
            }
            
            // Destroy all edge presenters
            foreach (var presenter in edgePresenterMap.Values)
            {
                if (presenter != null)
                {
                    DestroyImmediate(presenter.gameObject);
                }
            }
            
            nodePresenterMap.Clear();
            edgePresenterMap.Clear();
        }
        
        private NodePresenter CreateNodePresenter(NodeData nodeData)
        {
            GameObject nodeObj = new GameObject($"Node_{nodeData.displayName}");
            nodeObj.transform.SetParent(nodesContainer);
            
            NodePresenter presenter = nodeObj.AddComponent<NodePresenter>();
            presenter.network = networkAsset;
            presenter.nodeId = nodeData.id;
            presenter.ReloadNodeData();
            
            nodePresenterMap[nodeData.id] = presenter;
            return presenter;
        }
        
        private EdgePresenter CreateEdgePresenter(EdgeData edgeData)
        {
            GameObject edgeObj = new GameObject($"Edge_{edgeData.id.Substring(0, 5)}");
            edgeObj.transform.SetParent(edgesContainer);
            
            EdgePresenter presenter = edgeObj.AddComponent<EdgePresenter>();
            presenter.network = networkAsset;
            presenter.edgeId = edgeData.id;
            presenter.ReloadEdgeData();
            
            edgePresenterMap[edgeData.id] = presenter;
            return presenter;
        }
        
        // Helper methods for runtime use
        public NodePresenter GetNodePresenter(string nodeId)
        {
            if (nodePresenterMap.ContainsKey(nodeId))
            {
                return nodePresenterMap[nodeId];
            }
            return null;
        }
        
        public EdgePresenter GetEdgePresenter(string edgeId)
        {
            if (edgePresenterMap.ContainsKey(edgeId))
            {
                return edgePresenterMap[edgeId];
            }
            return null;
        }
        
        // Add a new node at specified position
        public NodePresenter AddNode(Vector3 position)
        {
            NodeData nodeData = networkAsset.AddNode(position);
            return CreateNodePresenter(nodeData);
        }
        
        // Add a new edge between nodes
        public EdgePresenter AddEdge(string startNodeId, string endNodeId)
        {
            EdgeData edgeData = networkAsset.AddEdge(startNodeId, endNodeId);
            if (edgeData == null)
                return null;
                
            return CreateEdgePresenter(edgeData);
        }
        
        // Find the closest node to a world position
        public NodePresenter FindClosestNodePresenter(Vector3 position, float maxDistance = 100f)
        {
            NodeData node = networkAsset.FindNearestNode(position, maxDistance);
            if (node != null && nodePresenterMap.ContainsKey(node.id))
            {
                return nodePresenterMap[node.id];
            }
            return null;
        }
    }
}