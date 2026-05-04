using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MiniGames.UI
{
    public enum JoystickType
    {
        Fixed,
        Floating
    }

    public class Joystick3D : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        // Public Events
        public event Action<Vector3, float> OnMovement;

        // Public Properties
        public Vector3 InputDirection => inputDirection;
        public bool IsMoving { get; private set; } = false;
        public bool IsInputLocked { get => lockInput; set => lockInput = value; }
        public bool IsBackgroundLocked { get => lockBackgroundPosition; set => lockBackgroundPosition = value; }

        [Header("Joystick Components")]
        [Tooltip("The background/frame of the joystick")]
        [SerializeField] private RectTransform joystickBackground;
        
        [Tooltip("The handle/stick that moves")]
        [SerializeField] private RectTransform joystickHandle;
        
        [Tooltip("Canvas group for fade effects")]
        [SerializeField] private CanvasGroup joystickCanvasGroup;

        [Header("Floating Mode Settings")]
        [Tooltip("For Floating mode: Use full screen touch area (recommended) or only joystick area")]
        [SerializeField] private bool useFullScreenTouchArea = true;
        
        [Tooltip("Optional: Custom touch area (leave empty for full screen). Only used when useFullScreenTouchArea is true")]
        [SerializeField] private RectTransform customTouchArea;
        
        [Tooltip("In Floating mode: Hide joystick when not in use. Uncheck to keep it always visible")]
        [SerializeField] private bool hideWhenNotInUse = true;

        [Header("Joystick Settings")]
        [Tooltip("Fixed: Stays in place. Floating: Appears at touch position")]
        [SerializeField] private JoystickType joystickType = JoystickType.Floating;
        
        [Tooltip("Maximum distance the handle can move from center")]
        [SerializeField] private float moveRange = 60f;
        
        [Tooltip("Lock joystick input (useful for cutscenes, menus, or disabling movement)")]
        [SerializeField] private bool lockInput = false;
        
        [Tooltip("Lock joystick background position (prevents it from moving to a new position in Floating mode)")]
        [SerializeField] private bool lockBackgroundPosition = false;

        [Header("Fade Settings")]
        [Tooltip("Enable fade effect when joystick is not in use")]
        [SerializeField] private bool enableFade = true;
        
        [Tooltip("Opacity when joystick is inactive (0-1)")]
        [SerializeField, Range(0, 1)] private float idleOpacity = 0.5f;
        
        [Tooltip("Opacity when joystick is active (0-1)")]
        [SerializeField, Range(0, 1)] private float activeOpacity = 1f;
        
        [Tooltip("Duration of fade transition in seconds")]
        [SerializeField] private float fadeDuration = 0.3f;
        
        [Tooltip("Delay before fading to idle opacity")]
        [SerializeField] private float fadeDelay = 0.5f;

        // Private variables
        private Vector3 inputDirection = Vector3.zero;
        private Vector2 startPosition;
        private Coroutine fadeCoroutine;
        private GameObject touchArea;
        private Canvas parentCanvas;

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (IsMoving)
            {
                ResetJoystick(false);
            }
        }

        private void Initialize()
        {
            // Validate required components
            if (joystickBackground == null || joystickHandle == null || joystickCanvasGroup == null)
            {
                Debug.LogError("Joystick3D: Missing required components! Please assign joystickBackground, joystickHandle, and joystickCanvasGroup in the inspector.");
                enabled = false;
                return;
            }

            // Get parent canvas reference
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogError("Joystick3D: Must be placed under a Canvas!");
                enabled = false;
                return;
            }

            // Store initial position for fixed mode
            startPosition = joystickBackground.anchoredPosition;

            // Set initial opacity
            if (enableFade)
            {
                joystickCanvasGroup.alpha = idleOpacity;
            }
            else
            {
                joystickCanvasGroup.alpha = 1f;
            }

            // For floating mode, hide the joystick initially (if hideWhenNotInUse is enabled) and create touch area
            if (joystickType == JoystickType.Floating)
            {
                if (hideWhenNotInUse)
                {
                    joystickCanvasGroup.alpha = 0f;
                }
                else if (enableFade)
                {
                    joystickCanvasGroup.alpha = idleOpacity;
                }
                
                if (useFullScreenTouchArea)
                {
                    CreateFullScreenTouchArea();
                }
            }
        }

        private void CreateFullScreenTouchArea()
        {
            // If custom touch area is provided, use it instead of creating one
            if (customTouchArea != null)
            {
                // Use the provided RectTransform as the touch area
                touchArea = customTouchArea.gameObject;
                
                // Ensure it has an Image component for raycasting
                Image existingImage = touchArea.GetComponent<Image>();
                if (existingImage == null)
                {
                    Image img = touchArea.AddComponent<Image>();
                    img.color = new Color(0, 0, 0, 0); // Completely transparent
                    img.raycastTarget = true;
                }
                else
                {
                    existingImage.raycastTarget = true;
                }
                
                // Add the touch handler component
                JoystickTouchHandler handler = touchArea.GetComponent<JoystickTouchHandler>();
                if (handler == null)
                {
                    handler = touchArea.AddComponent<JoystickTouchHandler>();
                }
                handler.joystick = this;
            }
            else
            {
                // Create a full-screen invisible GameObject to capture touch input
                touchArea = new GameObject("JoystickTouchArea");
                touchArea.transform.SetParent(parentCanvas.transform, false);
                touchArea.transform.SetSiblingIndex(0); // Put it at the bottom so it doesn't block other UI
                
                // Add RectTransform and stretch it to fill the screen
                RectTransform rect = touchArea.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                
                // Add an Image component (required for raycasting) but make it invisible
                Image img = touchArea.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0); // Completely transparent
                img.raycastTarget = true;
                
                // Add a component to handle the touch events
                JoystickTouchHandler handler = touchArea.AddComponent<JoystickTouchHandler>();
                handler.joystick = this;
            }
        }

        private void OnDestroy()
        {
            // Clean up the touch area when destroyed (only if it was created by us, not custom)
            if (touchArea != null && customTouchArea == null)
            {
                Destroy(touchArea);
            }
            else if (touchArea != null && customTouchArea != null)
            {
                // Remove the handler component from custom touch area
                JoystickTouchHandler handler = touchArea.GetComponent<JoystickTouchHandler>();
                if (handler != null)
                {
                    Destroy(handler);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsMoving || lockInput) return;

            IsMoving = true;
            StartJoystickSession(eventData);
        }

        private void StartJoystickSession(PointerEventData eventData)
        {
            // Stop any ongoing fade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            // Position joystick based on type and lock settings
            if (joystickType == JoystickType.Floating && !lockBackgroundPosition)
            {
                joystickBackground.position = eventData.position;
            }

            // Reset handle to center
            joystickHandle.anchoredPosition = Vector2.zero;
            inputDirection = Vector3.zero;

            // Set to active opacity
            if (enableFade)
            {
                fadeCoroutine = StartCoroutine(FadeCanvasGroup(activeOpacity, fadeDuration));
            }
            else
            {
                joystickCanvasGroup.alpha = 1f;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsMoving) return;

            // Calculate delta from joystick center
            Vector2 delta = eventData.position - (Vector2)joystickBackground.position;
            
            // Clamp to move range
            delta = Vector2.ClampMagnitude(delta, moveRange);

            // Update handle position
            joystickHandle.anchoredPosition = delta;

            // Calculate normalized input direction
            inputDirection = delta / moveRange;

            // Invoke movement event
            OnMovement?.Invoke(inputDirection, inputDirection.magnitude);

            // Ensure we're at full opacity while dragging
            if (enableFade && joystickCanvasGroup.alpha < activeOpacity)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                joystickCanvasGroup.alpha = activeOpacity;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetJoystick(true);
        }

        private void ResetJoystick(bool withFade)
        {
            IsMoving = false;

            // Reset handle to center
            joystickHandle.anchoredPosition = Vector2.zero;

            // Return background to start position (for fixed mode)
            if (joystickType == JoystickType.Fixed)
            {
                joystickBackground.anchoredPosition = startPosition;
            }

            // Clear input direction
            inputDirection = Vector3.zero;
            OnMovement?.Invoke(Vector3.zero, 0);

            // Handle fade effect
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (enableFade && withFade)
            {
                fadeCoroutine = StartCoroutine(FadeWithDelay(idleOpacity, fadeDelay, fadeDuration));
            }
            else if (enableFade)
            {
                joystickCanvasGroup.alpha = idleOpacity;
            }

            // For floating mode, hide completely when not in use (if hideWhenNotInUse is enabled)
            if (joystickType == JoystickType.Floating && hideWhenNotInUse)
            {
                if (withFade && enableFade)
                {
                    fadeCoroutine = StartCoroutine(FadeWithDelay(0f, fadeDelay, fadeDuration));
                }
                else
                {
                    joystickCanvasGroup.alpha = 0f;
                }
            }
        }

        private IEnumerator FadeWithDelay(float targetAlpha, float delay, float duration)
        {
            yield return new WaitForSeconds(delay);
            yield return FadeCanvasGroup(targetAlpha, duration);
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            float startAlpha = joystickCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                joystickCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            joystickCanvasGroup.alpha = targetAlpha;
        }

        // Public API methods
        public void SetJoystickType(JoystickType type)
        {
            joystickType = type;
            
            // Clean up existing touch area
            if (touchArea != null)
            {
                Destroy(touchArea);
                touchArea = null;
            }
            
            if (!IsMoving)
            {
                if (type == JoystickType.Fixed)
                {
                    joystickBackground.anchoredPosition = startPosition;
                    if (enableFade)
                    {
                        joystickCanvasGroup.alpha = idleOpacity;
                    }
                }
                else // Floating
                {
                    if (hideWhenNotInUse)
                    {
                        joystickCanvasGroup.alpha = 0f;
                    }
                    else if (enableFade)
                    {
                        joystickCanvasGroup.alpha = idleOpacity;
                    }
                    
                    if (useFullScreenTouchArea)
                    {
                        CreateFullScreenTouchArea();
                    }
                }
            }
        }

        public void SetFadeEnabled(bool enabled)
        {
            enableFade = enabled;
            if (!enabled)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    fadeCoroutine = null;
                }
                joystickCanvasGroup.alpha = 1f;
            }
        }

        public void SetHideWhenNotInUse(bool hide)
        {
            hideWhenNotInUse = hide;
            
            // Update visibility immediately if not currently moving
            if (!IsMoving && joystickType == JoystickType.Floating)
            {
                if (hide)
                {
                    joystickCanvasGroup.alpha = 0f;
                }
                else if (enableFade)
                {
                    joystickCanvasGroup.alpha = idleOpacity;
                }
                else
                {
                    joystickCanvasGroup.alpha = 1f;
                }
            }
        }

        public void LockPosition(bool locked)
        {
            lockInput = locked;
            
            // If locking while moving, reset the joystick
            if (locked && IsMoving)
            {
                ResetJoystick(true);
            }
        }

        public void LockInput(bool locked)
        {
            lockInput = locked;
            
            // If locking while moving, reset the joystick
            if (locked && IsMoving)
            {
                ResetJoystick(true);
            }
        }

        public void LockBackground(bool locked)
        {
            lockBackgroundPosition = locked;
        }

        public void Lock()
        {
            LockInput(true);
        }

        public void Unlock()
        {
            LockInput(false);
        }

        public void LockBackgroundPosition()
        {
            lockBackgroundPosition = true;
        }

        public void UnlockBackgroundPosition()
        {
            lockBackgroundPosition = false;
        }

        // Internal methods called by touch handler
        internal void HandleTouchAreaPointerDown(PointerEventData eventData)
        {
            OnPointerDown(eventData);
        }

        internal void HandleTouchAreaDrag(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        internal void HandleTouchAreaPointerUp(PointerEventData eventData)
        {
            OnPointerUp(eventData);
        }
    }

    /// <summary>
    /// Internal component for handling full-screen touch input in Floating mode
    /// </summary>
    internal class JoystickTouchHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        internal Joystick3D joystick;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (joystick != null && !joystick.IsMoving)
            {
                joystick.HandleTouchAreaPointerDown(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (joystick != null && joystick.IsMoving)
            {
                joystick.HandleTouchAreaDrag(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (joystick != null && joystick.IsMoving)
            {
                joystick.HandleTouchAreaPointerUp(eventData);
            }
        }
    }
}
