using System;
using UnityEngine;

namespace UrbanSim
{
    [Serializable]
    public class EdgeData
    {
        public string id;
        public string startNodeId;
        public string endNodeId;
        
        // Transport properties
        public TransportMode allowedModes = TransportMode.Driving;
        public bool isOneWay = false;
        
        // Visual properties
        public float curvature = 0f;  // 0 = straight line, >0 = increasing curve
        
        // Optional properties for future expansion
        public float speedLimit = 50f;  // km/h
        public int laneCount = 1;
        
        // Cached/computed values (not serialized)
        [NonSerialized]
        public float length;
        
        // Constructor
        public EdgeData(string startNodeId, string endNodeId)
        {
            this.id = Guid.NewGuid().ToString();
            this.startNodeId = startNodeId;
            this.endNodeId = endNodeId;
        }
        
        // Calculate length based on node positions (requires network reference)
        public void CalculateLength(TransportNetwork network)
        {
            NodeData startNode = network.GetNodeById(startNodeId);
            NodeData endNode = network.GetNodeById(endNodeId);
            
            if (startNode != null && endNode != null)
            {
                length = Vector3.Distance(startNode.position, endNode.position);
            }
            else
            {
                Debug.LogWarning($"Could not calculate length for edge {id}: One or both nodes missing");
                length = 0f;
            }
        }
    }
}