using UnityEngine;

namespace UrbanSim
{
    [ExecuteInEditMode]
    public class EdgePresenter : MonoBehaviour
    {
        public string edgeId;
        public TransportNetwork network;
        public float lineWidth = 0.2f;
        public int curveResolution = 20;
        
        // References
        private EdgeData edgeData;
        private LineRenderer lineRenderer;
        private Transform startNodeTransform;
        private Transform endNodeTransform;
        private bool initialized = false;
        
        private void OnEnable()
        {
            // Set up line renderer if it doesn't exist
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                lineRenderer.useWorldSpace = true;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
            }
            
            ReloadEdgeData();
        }
        
        public void ReloadEdgeData()
        {
            if (network == null || string.IsNullOrEmpty(edgeId))
                return;
                
            edgeData = network.GetEdgeById(edgeId);
            if (edgeData != null)
            {
                initialized = true;
                UpdateReferences();
                UpdateVisuals();
            }
        }
        
        private void Update()
        {
            if (!initialized)
                return;
                
            // Update visuals if node positions have changed
            if (startNodeTransform != null && endNodeTransform != null)
            {
                UpdateVisuals();
            }
            else
            {
                // Try to reacquire node references if they're missing
                UpdateReferences();
            }
        }
        
        private void UpdateReferences()
        {
            if (edgeData == null)
                return;
                
            // Find node presenters for start and end nodes
            NodePresenter[] nodePresenters = FindObjectsOfType<NodePresenter>();
            
            foreach (var nodePresenter in nodePresenters)
            {
                if (nodePresenter.nodeId == edgeData.startNodeId)
                {
                    startNodeTransform = nodePresenter.transform;
                }
                else if (nodePresenter.nodeId == edgeData.endNodeId)
                {
                    endNodeTransform = nodePresenter.transform;
                }
            }
            
            // Update name
            gameObject.name = "Edge " + edgeId.Substring(0, 5);
        }
        
        private void UpdateVisuals()
        {
            if (edgeData == null || startNodeTransform == null || endNodeTransform == null || lineRenderer == null)
                return;
                
            // Update line renderer positions
            if (edgeData.curvature > 0)
            {
                // Create curved path
                lineRenderer.positionCount = curveResolution;
                Vector3 startPos = startNodeTransform.position;
                Vector3 endPos = endNodeTransform.position;
                
                // Calculate midpoint and offset it for curve
                Vector3 midPoint = (startPos + endPos) / 2f;
                Vector3 direction = (endPos - startPos).normalized;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                midPoint += perpendicular * edgeData.curvature;
                
                // Create bezier points
                for (int i = 0; i < curveResolution; i++)
                {
                    float t = i / (float)(curveResolution - 1);
                    Vector3 point = QuadraticBezier(startPos, midPoint, endPos, t);
                    lineRenderer.SetPosition(i, point);
                }
            }
            else
            {
                // Simple straight line
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, startNodeTransform.position);
                lineRenderer.SetPosition(1, endNodeTransform.position);
            }
            
            // Set color and width based on transport modes
            Color edgeColor;
            float edgeWidth = lineWidth;
            
            if (edgeData.allowedModes == TransportMode.None)
            {
                // Invalid edge (no transport modes)
                edgeColor = Color.gray;
                edgeWidth = lineWidth * 0.5f;
            }
            else if (edgeData.allowedModes == TransportMode.Walking)
            {
                // Walking only (blue)
                edgeColor = new Color(0.2f, 0.6f, 1.0f);
                edgeWidth = lineWidth * 0.7f;
            }
            else if (edgeData.allowedModes == TransportMode.Driving)
            {
                // Driving only (black)
                edgeColor = Color.black;
                edgeWidth = lineWidth;
            }
            else if (edgeData.allowedModes == (TransportMode.Driving | TransportMode.Walking))
            {
                // Both modes (purple)
                edgeColor = new Color(0.6f, 0.2f, 0.8f);
                edgeWidth = lineWidth * 1.2f;
            }
            else
            {
                // Other combinations (for future modes)
                edgeColor = Color.cyan;
                edgeWidth = lineWidth;
            }
            
            // Apply visual changes
            lineRenderer.startWidth = edgeWidth;
            lineRenderer.endWidth = edgeWidth;
            
            if (edgeData.isOneWay)
            {
                // One-way streets have a gradient from dark to light
                lineRenderer.startColor = new Color(edgeColor.r * 0.7f, edgeColor.g * 0.7f, edgeColor.b * 0.7f);
                lineRenderer.endColor = edgeColor;
            }
            else
            {
                // Two-way streets are solid color
                lineRenderer.startColor = edgeColor;
                lineRenderer.endColor = edgeColor;
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
    }
}