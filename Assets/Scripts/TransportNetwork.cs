using System.Collections.Generic;
using UnityEngine;

namespace UrbanSim
{
    [CreateAssetMenu(fileName = "TransportNetwork", menuName = "Urban Simulation/Transport Network")]
    public class TransportNetwork : ScriptableObject
    {
        [SerializeField]
        private List<NodeData> nodes = new List<NodeData>();
        
        [SerializeField]
        private List<EdgeData> edges = new List<EdgeData>();
        
        // Node operations
        public NodeData AddNode(Vector3 position)
        {
            NodeData newNode = new NodeData(position);
            nodes.Add(newNode);
            return newNode;
        }
        
        public bool RemoveNode(string nodeId)
        {
            // First, remove any edges connected to this node
            edges.RemoveAll(e => e.startNodeId == nodeId || e.endNodeId == nodeId);
            
            // Then remove the node itself
            return nodes.RemoveAll(n => n.id == nodeId) > 0;
        }
        
        public NodeData GetNodeById(string id)
        {
            return nodes.Find(n => n.id == id);
        }
        
        // Edge operations
        public EdgeData AddEdge(string startNodeId, string endNodeId)
        {
            // Verify nodes exist
            if (GetNodeById(startNodeId) == null || GetNodeById(endNodeId) == null)
            {
                Debug.LogError("Cannot create edge: one or both nodes don't exist");
                return null;
            }
            
            // Check if this edge already exists
            if (edges.Exists(e => 
                (e.startNodeId == startNodeId && e.endNodeId == endNodeId) || 
                (e.startNodeId == endNodeId && e.endNodeId == startNodeId)))
            {
                Debug.LogWarning("Edge between these nodes already exists");
                return null;
            }
            
            EdgeData newEdge = new EdgeData(startNodeId, endNodeId);
            newEdge.CalculateLength(this);
            edges.Add(newEdge);
            return newEdge;
        }
        
        public bool RemoveEdge(string edgeId)
        {
            return edges.RemoveAll(e => e.id == edgeId) > 0;
        }
        
        public EdgeData GetEdgeById(string id)
        {
            return edges.Find(e => e.id == id);
        }
        
        // Get all edges connected to a node
        public List<EdgeData> GetEdgesForNode(string nodeId)
        {
            return edges.FindAll(e => e.startNodeId == nodeId || e.endNodeId == nodeId);
        }
        
        // Get all nodes
        public List<NodeData> GetAllNodes()
        {
            return new List<NodeData>(nodes);
        }
        
        // Get all edges
        public List<EdgeData> GetAllEdges()
        {
            return new List<EdgeData>(edges);
        }
        
        // Update all edge lengths (call after node positions change)
        public void UpdateEdgeLengths()
        {
            foreach (EdgeData edge in edges)
            {
                edge.CalculateLength(this);
            }
        }
        
        // Get edges with specific transport mode(s)
        public List<EdgeData> GetEdgesByTransportMode(TransportMode mode)
        {
            return edges.FindAll(e => (e.allowedModes & mode) == mode);
        }
        
        // Find nearest node to a position
        public NodeData FindNearestNode(Vector3 position, float maxDistance = float.MaxValue)
        {
            NodeData nearestNode = null;
            float shortestDistance = maxDistance;
            
            foreach (NodeData node in nodes)
            {
                float distance = Vector3.Distance(position, node.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestNode = node;
                }
            }
            
            return nearestNode;
        }
    }
}