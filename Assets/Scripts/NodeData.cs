using System;
using UnityEngine;

namespace UrbanSim
{
    [Serializable]
    public class NodeData
    {
        public string id;
        public Vector3 position;
        public string displayName;
        
        // Optional type field for future expansion (intersections, POIs, etc.)
        public NodeType nodeType = NodeType.Intersection;
        
        // Constructor for new nodes
        public NodeData(Vector3 position)
        {
            this.id = Guid.NewGuid().ToString();
            this.position = position;
            this.displayName = "Node " + id.Substring(0, 5);
        }
        
        // Constructor with custom ID (for loading from saved data)
        public NodeData(string id, Vector3 position)
        {
            this.id = id;
            this.position = position;
            this.displayName = "Node " + id.Substring(0, 5);
        }
    }
    
    public enum NodeType
    {
        Intersection,
        Building,
        ParkingLot,
        BusStop
    }
}