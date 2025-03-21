using UnityEngine;

namespace UrbanTransport
{
    /// <summary>
    /// Simple visual effect for destination markers
    /// </summary>
    public class DestinationMarker : MonoBehaviour
    {
        [Header("Visual Settings")]
        public float pulseSpeed = 1.5f;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;
        public Color markerColor = Color.green;
        
        // Components
        private Renderer markerRenderer;
        
        // Start is called before the first frame update
        void Start()
        {
            // Set up the marker
            markerRenderer = GetComponent<Renderer>();
            
            if (markerRenderer != null)
            {
                markerRenderer.material.color = markerColor;
            }
            
            // Position slightly above ground to avoid Z-fighting
            transform.position = new Vector3(
                transform.position.x, 
                transform.position.y + 0.05f, 
                transform.position.z
            );
        }
        
        // Update is called once per frame
        void Update()
        {
            // Calculate pulse effect
            float scale = Mathf.Lerp(minScale, maxScale, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
                
            // Apply scale
            transform.localScale = new Vector3(scale, scale, scale);
            
            // Ensure marker stays visible by facing camera
            if (Camera.main != null)
            {
                transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up);
            }
        }
    }
}