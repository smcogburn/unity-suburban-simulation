using UnityEngine;
using UnityEngine.AI;

 /// <summary>
    /// Allows clicking in the scene to set agent destinations
    /// </summary>
    public class NavigationTest : MonoBehaviour
    {
        [Header("Agent Reference")]
        public EnhancedAgentBehavior agent;

        [Header("Click Settings")]
        public LayerMask clickableLayers = Physics.DefaultRaycastLayers;
        public float maxClickDistance = 100f;
        
        [Header("Visual Feedback")]
        public GameObject destinationMarkerPrefab;
        public float markerDuration = 3f;
        
        // Current destination marker
        private GameObject currentMarker;
        
        // Start is called before the first frame update
        void Start()
        {
            // Find agent if not assigned
            if (agent == null)
            {
                agent = FindObjectOfType<EnhancedAgentBehavior>();
                
                if (agent == null)
                {
                    Debug.LogError("No EnhancedAgentBehavior found in scene. Please assign one.");
                }
            }
        }
        
        // Update is called once per frame
        void Update()
        {
            // Check for mouse click
            if (Input.GetMouseButtonDown(0))
            {
                // Create a ray from the mouse position
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                // Perform raycast
                if (Physics.Raycast(ray, out hit, maxClickDistance, clickableLayers))
                {
                    // Set the agent's destination
                    SetDestination(hit.point);
                }
            }
        }
        
        /// <summary>
        /// Set the agent's destination and show visual feedback
        /// </summary>
        public void SetDestination(Vector3 position)
        {
            // Only proceed if we have a valid agent
            if (agent == null)
                return;
                
            // Set the destination
            agent.SetDestination(position);
            
            // Show visual feedback
            ShowDestinationMarker(position);
            
            Debug.Log($"Navigating to {position}");
        }
        
        /// <summary>
        /// Show a marker at the destination
        /// </summary>
        private void ShowDestinationMarker(Vector3 position)
        {
            // Clear any existing marker
            if (currentMarker != null)
            {
                Destroy(currentMarker);
            }
            
            // Create a new marker if we have a prefab
            if (destinationMarkerPrefab != null)
            {
                currentMarker = Instantiate(destinationMarkerPrefab, position, Quaternion.identity);
                
                // Destroy after duration
                Destroy(currentMarker, markerDuration);
            }
            else
            {
                // Simple debug visualization if no prefab
                Debug.DrawRay(position, Vector3.up * 2f, Color.green, markerDuration);
            }
        }
    }