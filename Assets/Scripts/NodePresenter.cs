using UnityEngine;

namespace UrbanSim
{
    [ExecuteInEditMode]
    public class NodePresenter : MonoBehaviour
    {
        public string nodeId;
        public TransportNetwork network;
        public float nodeRadius = 0.5f;
        
        private NodeData nodeData;
        private MeshRenderer meshRenderer;
        private bool initialized = false;
        
        private void OnEnable()
        {
            // Set up basic visuals if they don't exist
            if (GetComponent<MeshFilter>() == null)
            {
                var filter = gameObject.AddComponent<MeshFilter>();
                filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            }
            
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.white;
                meshRenderer.sharedMaterial = mat;
            }
            
            ReloadNodeData();
        }
        
        public void ReloadNodeData()
        {
            if (network == null || string.IsNullOrEmpty(nodeId))
                return;
                
            nodeData = network.GetNodeById(nodeId);
            if (nodeData != null)
            {
                initialized = true;
                UpdateVisuals();
            }
        }
        
        private void Update()
        {
            if (!initialized)
                return;
            
            // In edit mode, sync position changes back to data
            #if UNITY_EDITOR
            if (!Application.isPlaying && nodeData != null && transform.hasChanged)
            {
                nodeData.position = transform.position;
                transform.hasChanged = false;
                
                // Update connected edges' lengths
                network.UpdateEdgeLengths();
            }
            #endif
            
            // Sync data position to transform if needed
            if (nodeData != null && Vector3.Distance(transform.position, nodeData.position) > 0.01f)
            {
                transform.position = nodeData.position;
            }
            
            // Update visuals based on connection status
            UpdateNodeAppearance();
        }
        
        private void UpdateVisuals()
        {
            if (nodeData == null)
                return;
                
            // Update transform
            transform.position = nodeData.position;
            transform.localScale = Vector3.one * nodeRadius * 2f;
            
            // Update name
            gameObject.name = nodeData.displayName;
        }
        
        private void UpdateNodeAppearance()
        {
            if (network == null || nodeData == null || meshRenderer == null)
                return;
                
            // Get all edges connected to this node
            var connectedEdges = network.GetEdgesForNode(nodeId);
            
            // Determine node color based on connections
            Color nodeColor;
            
            if (connectedEdges.Count == 0)
            {
                // Disconnected node
                nodeColor = Color.red;
            }
            else
            {
                // Check what transport modes are available from this node
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
                    // Mixed node (both transport types)
                    nodeColor = new Color(0.6f, 0.3f, 0.8f); // Purple
                }
                else if (hasWalking)
                {
                    // Walking only
                    nodeColor = new Color(0.3f, 0.7f, 1.0f); // Blue
                }
                else if (hasDriving)
                {
                    // Driving only
                    nodeColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray
                }
                else
                {
                    // Should never happen, but just in case
                    nodeColor = Color.yellow;
                }
            }
            
            // Apply the color
            meshRenderer.material.color = nodeColor;
        }
    }
}