using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MiniGames.UI
{
    public enum LayerType
    {
        FrontLayer,
        BackLayer
    }

    public class HiddenObjectGameManager : MonoBehaviour, IMiniGame
    {
        // Public Events
        public event Action OnWin;

        // Public Properties (Interface Implementation)
        public bool IsGameActive => isGameActive;
        public UnityEvent OnWinEvent => _onWinEvent;

        [Header("Events")]
        [Tooltip("Unity Event fired when player wins (for inspector configuration)")]
        [SerializeField] private UnityEvent _onWinEvent;

        [Header("Layer References")]
        [Tooltip("Front sprite layer that will move with joystick")]
        [SerializeField] private GameObject frontLayerObject;
        
        [Tooltip("Back sprite layer that stays fixed")]
        [SerializeField] private GameObject backLayerObject;

        [Header("Joystick Settings")]
        [Tooltip("Reference to the Joystick3D component")]
        [SerializeField] private Joystick3D joystick;
        
        [Tooltip("Speed at which the front layer moves")]
        [SerializeField] private float moveSpeed = 3f;

        [Header("Game Settings")]
        [Tooltip("Which layer is the target to find and click")]
        [SerializeField] private LayerType targetLayer = LayerType.BackLayer;
        
        [Tooltip("Cover object that hides/shows the game")]
        [SerializeField] private GameObject coverObject;

        [Header("Movement Boundaries")]
        [Tooltip("Enable to limit the front layer movement range")]
        [SerializeField] private bool limitMovementRange = false;
        
        [Tooltip("Minimum offset from start position (e.g., -5 means can move 5 units left/down from start)")]
        [SerializeField] private Vector2 minBounds = new Vector2(-5f, -3f);
        
        [Tooltip("Maximum offset from start position (e.g., 5 means can move 5 units right/up from start)")]
        [SerializeField] private Vector2 maxBounds = new Vector2(5f, 3f);

        [Header("Parallax Settings")]
        [Tooltip("Enable back layer to move relative to front layer (parallax effect)")]
        [SerializeField] private bool enableParallax = false;
        
        [Tooltip("Movement ratio for back layer on X axis (0 = no movement, 1 = same as front, 0.5 = half speed)")]
        [SerializeField, Range(0f, 1f)] private float parallaxRatioX = 0.5f;
        
        [Tooltip("Movement ratio for back layer on Y axis (0 = no movement, 1 = same as front, 0.5 = half speed)")]
        [SerializeField, Range(0f, 1f)] private float parallaxRatioY = 0.5f;

        // Private variables
        private bool isGameActive = false;
        private Vector3 currentMovement = Vector3.zero;
        private LayerClickDetector frontLayer;
        private LayerClickDetector backLayer;
        private Vector3 frontLayerStartPosition;
        private Vector3 backLayerStartPosition;

        private void Awake()
        {
            // Validate required components
            if (!ValidateComponents())
            {
                enabled = false;
                return;
            }

            // Store original start positions from scene setup
            if (frontLayerObject != null)
            {
                frontLayerStartPosition = frontLayerObject.transform.position;
                Debug.Log($"Front layer start position stored: {frontLayerStartPosition}");
            }
            
            if (backLayerObject != null)
            {
                backLayerStartPosition = backLayerObject.transform.position;
                Debug.Log($"Back layer start position stored: {backLayerStartPosition}");
            }

            // Get LayerClickDetector components
            if (frontLayerObject != null)
            {
                frontLayer = frontLayerObject.GetComponent<LayerClickDetector>();
                if (frontLayer == null)
                {
                    frontLayer = frontLayerObject.AddComponent<LayerClickDetector>();
                    Debug.Log($"Added LayerClickDetector to {frontLayerObject.name}");
                }
            }
            
            if (backLayerObject != null)
            {
                backLayer = backLayerObject.GetComponent<LayerClickDetector>();
                if (backLayer == null)
                {
                    backLayer = backLayerObject.AddComponent<LayerClickDetector>();
                    Debug.Log($"Added LayerClickDetector to {backLayerObject.name}");
                }
            }

            // Ensure camera has Physics2DRaycaster for 2D click detection
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
            {
                cam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
                Debug.Log("Added Physics2DRaycaster to main camera for 2D click detection");
            }
        }

        private void Start()
        {
            // Start with game hidden if cover is assigned
            if (coverObject != null)
            {
                coverObject.SetActive(true);
            }
        }

        private void Update()
        {
            // Apply movement if game is active
            if (isGameActive && frontLayerObject != null && currentMovement.magnitude > 0.01f)
            {
                MoveFrontLayer();
            }
        }

        private bool ValidateComponents()
        {
            bool isValid = true;

            if (frontLayerObject == null)
            {
                Debug.LogError("HiddenObjectGameManager: Front Layer is not assigned!");
                isValid = false;
            }
            else if (frontLayerObject.GetComponent<Collider2D>() == null)
            {
                Debug.LogError("HiddenObjectGameManager: Front Layer needs a Collider2D component!");
                isValid = false;
            }

            if (backLayerObject == null)
            {
                Debug.LogError("HiddenObjectGameManager: Back Layer is not assigned!");
                isValid = false;
            }
            else if (backLayerObject.GetComponent<Collider2D>() == null)
            {
                Debug.LogError("HiddenObjectGameManager: Back Layer needs a Collider2D component!");
                isValid = false;
            }

            if (joystick == null)
            {
                Debug.LogError("HiddenObjectGameManager: Joystick3D is not assigned!");
                isValid = false;
            }

            return isValid;
        }

        private void MoveFrontLayer()
        {
            Debug.Log($"=== MoveFrontLayer START ===");
            Debug.Log($"Front layer before move: {frontLayerObject.transform.position}");
            Debug.Log($"Front start position: {frontLayerStartPosition}");
            Debug.Log($"Current movement: {currentMovement}");
            
            // Calculate movement based on joystick input
            Vector3 movement = new Vector3(currentMovement.x, currentMovement.y, 0f) * moveSpeed * Time.deltaTime;
            Debug.Log($"Calculated movement delta: {movement}");
            
            // Apply movement to front layer
            Vector3 newPosition = frontLayerObject.transform.position + movement;
            Debug.Log($"New position (before clamp): {newPosition}");

            // Clamp position if boundaries are enabled (relative to start position)
            if (limitMovementRange)
            {
                // Calculate absolute boundaries relative to start position
                float minX = frontLayerStartPosition.x + minBounds.x;
                float maxX = frontLayerStartPosition.x + maxBounds.x;
                float minY = frontLayerStartPosition.y + minBounds.y;
                float maxY = frontLayerStartPosition.y + maxBounds.y;
                
                Debug.Log($"Absolute bounds: X({minX} to {maxX}), Y({minY} to {maxY})");
                
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
                Debug.Log($"New position (after clamp): {newPosition}");
            }

            frontLayerObject.transform.position = newPosition;
            Debug.Log($"Front layer after setting: {frontLayerObject.transform.position}");

            // Apply parallax movement to back layer if enabled
            if (enableParallax && backLayerObject != null)
            {
                // Calculate the total displacement of front layer from its start position
                Vector3 frontDisplacement = frontLayerObject.transform.position - frontLayerStartPosition;
                Debug.Log($"Front displacement from start: {frontDisplacement}");
                
                // Apply different parallax ratios to X and Y axes
                Vector3 backParallaxDisplacement = new Vector3(
                    frontDisplacement.x * parallaxRatioX,
                    frontDisplacement.y * parallaxRatioY,
                    frontDisplacement.z
                );
                Debug.Log($"Back parallax displacement: {backParallaxDisplacement}");
                
                // Set back layer position relative to its start position
                Vector3 backNewPos = backLayerStartPosition + backParallaxDisplacement;
                Debug.Log($"Back layer new position: {backNewPos}");
                backLayerObject.transform.position = backNewPos;
                Debug.Log($"Back layer after setting: {backLayerObject.transform.position}");
            }
            Debug.Log($"=== MoveFrontLayer END ===\n");
        }

        private void OnLayerClickedHandler(LayerClickDetector clickedLayer)
        {
            Debug.Log($"=== OnLayerClickedHandler CALLED ===");
            Debug.Log($"Layer clicked: {clickedLayer.gameObject.name}");
            Debug.Log($"frontLayer reference: {(frontLayer != null ? frontLayer.gameObject.name : "NULL")}");
            Debug.Log($"backLayer reference: {(backLayer != null ? backLayer.gameObject.name : "NULL")}");
            Debug.Log($"clickedLayer == frontLayer: {clickedLayer == frontLayer}");
            Debug.Log($"clickedLayer == backLayer: {clickedLayer == backLayer}");
            
            // Determine which layer was clicked
            LayerType clickedLayerType;
            
            if (clickedLayer == frontLayer)
            {
                clickedLayerType = LayerType.FrontLayer;
                Debug.Log($"✓ Identified as FRONT layer! Target is: {targetLayer}");
            }
            else if (clickedLayer == backLayer)
            {
                clickedLayerType = LayerType.BackLayer;
                Debug.Log($"✓ Identified as BACK layer! Target is: {targetLayer}");
            }
            else
            {
                Debug.LogWarning($"❌ Unknown layer clicked: {clickedLayer.gameObject.name}");
                return;
            }
            
            // Check if correct layer was clicked
            Debug.Log($"Comparing: clickedLayerType={clickedLayerType} vs targetLayer={targetLayer}");
            
            if (clickedLayerType == targetLayer)
            {
                // Correct layer clicked - player wins!
                Debug.Log("🎉 Player Won! Target layer found.");
                Debug.Log($"Invoking OnWin event (subscribers: {(OnWin != null ? OnWin.GetInvocationList().Length : 0)})");
                Debug.Log($"Invoking onWinEvent (has listeners: {(_onWinEvent != null && _onWinEvent.GetPersistentEventCount() > 0)})");
                
                OnWin?.Invoke();
                _onWinEvent?.Invoke();
                
                Debug.Log("Events invoked!");
            }
            else
            {
                // Wrong layer clicked
                Debug.Log("❌ Wrong layer clicked. Keep searching!");
            }
        }

        private void OnJoystickMovement(Vector3 direction, float magnitude)
        {
            currentMovement = direction;
            Debug.Log($"Joystick movement: {direction}, magnitude: {magnitude}");
            Debug.Log($"Front layer current pos: {frontLayerObject.transform.position}, start pos: {frontLayerStartPosition}");
            Debug.Log($"Back layer current pos: {backLayerObject.transform.position}, start pos: {backLayerStartPosition}");
        }

        // Public API Methods

        /// <summary>
        /// Shows the game by hiding the cover and enabling gameplay
        /// </summary>
        public void ShowGame()
        {
            if (!enabled) return;

            // Reset movement to zero first
            currentMovement = Vector3.zero;

            // Update and reset layers to their current scene positions
            // This captures wherever they are NOW as the start position
            if (frontLayerObject != null)
            {
                frontLayerStartPosition = frontLayerObject.transform.position;
                Debug.Log($"Front layer start position updated and set to: {frontLayerStartPosition}");
            }
            if (backLayerObject != null)
            {
                backLayerStartPosition = backLayerObject.transform.position;
                Debug.Log($"Back layer start position updated and set to: {backLayerStartPosition}");
            }

            // Hide the cover
            if (coverObject != null)
            {
                coverObject.SetActive(false);
            }

            // Show the joystick
            if (joystick != null)
            {
                joystick.gameObject.SetActive(true);
                Debug.Log("Joystick shown");
            }

            // Subscribe to layer click events
            if (frontLayer != null)
            {
                frontLayer.OnClicked += OnLayerClickedHandler;
                Debug.Log($"✓ Subscribed to front layer ({frontLayerObject.name}) click events");
            }
            else
            {
                Debug.LogWarning("❌ Front layer detector is NULL - cannot subscribe!");
            }
            
            if (backLayer != null)
            {
                backLayer.OnClicked += OnLayerClickedHandler;
                Debug.Log($"✓ Subscribed to back layer ({backLayerObject.name}) click events");
            }
            else
            {
                Debug.LogWarning("❌ Back layer detector is NULL - cannot subscribe!");
            }

            // Subscribe to joystick movement
            if (joystick != null)
            {
                joystick.OnMovement += OnJoystickMovement;
            }

            isGameActive = true;
            Debug.Log("HiddenObjectGameManager: Game started!");
        }

        /// <summary>
        /// Hides the game by showing the cover and disabling gameplay
        /// </summary>
        public void HideGame()
        {
            // Show the cover
            if (coverObject != null)
            {
                coverObject.SetActive(true);
            }

            // Hide the joystick
            if (joystick != null)
            {
                joystick.gameObject.SetActive(false);
                Debug.Log("Joystick hidden");
            }

            // Unsubscribe from layer click events
            if (frontLayer != null)
            {
                frontLayer.OnClicked -= OnLayerClickedHandler;
            }
            if (backLayer != null)
            {
                backLayer.OnClicked -= OnLayerClickedHandler;
            }

            // Unsubscribe from joystick movement
            if (joystick != null)
            {
                joystick.OnMovement -= OnJoystickMovement;
            }

            // Reset positions to start positions
            if (frontLayerObject != null)
            {
                frontLayerObject.transform.position = frontLayerStartPosition;
            }
            if (backLayerObject != null)
            {
                backLayerObject.transform.position = backLayerStartPosition;
            }

            // Reset movement
            currentMovement = Vector3.zero;
            isGameActive = false;
            Debug.Log("HiddenObjectGameManager: Game hidden!");
        }

        /// <summary>
        /// Resets the game to initial state while keeping it active.
        /// Resets layer positions without hiding the cover or unsubscribing from events.
        /// </summary>
        public void ResetGame()
        {
            if (!isGameActive)
            {
                Debug.LogWarning("HiddenObjectGameManager: Cannot reset - game is not active!");
                return;
            }

            // Reset movement to zero
            currentMovement = Vector3.zero;

            // Reset layers to their start positions
            if (frontLayerObject != null)
            {
                frontLayerObject.transform.position = frontLayerStartPosition;
                Debug.Log($"Front layer reset to start position: {frontLayerStartPosition}");
            }
            if (backLayerObject != null)
            {
                backLayerObject.transform.position = backLayerStartPosition;
                Debug.Log($"Back layer reset to start position: {backLayerStartPosition}");
            }

            Debug.Log("HiddenObjectGameManager: Game reset!");
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (frontLayer != null)
            {
                frontLayer.OnClicked -= OnLayerClickedHandler;
            }
            if (backLayer != null)
            {
                backLayer.OnClicked -= OnLayerClickedHandler;
            }
            if (joystick != null)
            {
                joystick.OnMovement -= OnJoystickMovement;
            }
        }

        private void OnDisable()
        {
            // Ensure we unsubscribe if disabled
            if (frontLayer != null)
            {
                frontLayer.OnClicked -= OnLayerClickedHandler;
            }
            if (backLayer != null)
            {
                backLayer.OnClicked -= OnLayerClickedHandler;
            }
            if (joystick != null)
            {
                joystick.OnMovement -= OnJoystickMovement;
            }
        }

        // Gizmos for visualizing movement boundaries in editor
        private void OnDrawGizmosSelected()
        {
            if (limitMovementRange && frontLayerObject != null)
            {
                Gizmos.color = Color.yellow;
                
                // Use start position if available, otherwise use current position
                Vector3 startPos = Application.isPlaying ? frontLayerStartPosition : frontLayerObject.transform.position;
                
                // Calculate absolute boundary positions relative to start position
                Vector3 center = new Vector3(
                    startPos.x + (minBounds.x + maxBounds.x) / 2f,
                    startPos.y + (minBounds.y + maxBounds.y) / 2f,
                    startPos.z
                );
                
                Vector3 size = new Vector3(
                    maxBounds.x - minBounds.x,
                    maxBounds.y - minBounds.y,
                    0f
                );
                
                Gizmos.DrawWireCube(center, size);
                
                // Draw start position marker
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startPos, 0.1f);
            }
        }
    }
}


