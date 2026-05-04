using UnityEngine;
using MiniGames.UI;

/// <summary>
/// Example script showing how to use the Joystick3D component.
/// Attach this to a GameObject that you want to control with the joystick.
/// </summary>
public class JoystickExample : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the Joystick3D component")]
    [SerializeField] private Joystick3D joystick;

    [Header("Movement Settings")]
    [Tooltip("Movement speed of the controlled object")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Should the object rotate to face movement direction?")]
    [SerializeField] private bool rotateTowardsDirection = true;

    [Tooltip("Speed of rotation towards movement direction")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Movement Type")]
    [Tooltip("Move in 3D space (XZ plane) or 2D space (XY plane)")]
    [SerializeField] private bool use3DMovement = true;

    private void OnEnable()
    {
        // Subscribe to joystick movement events
        if (joystick != null)
        {
            joystick.OnMovement += HandleMovement;
        }
        else
        {
            Debug.LogError("JoystickExample: No joystick reference assigned!");
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from joystick events
        if (joystick != null)
        {
            joystick.OnMovement -= HandleMovement;
        }
    }

    private void HandleMovement(Vector3 direction, float magnitude)
    {
        // direction: normalized input direction from joystick (-1 to 1 for x and y)
        // magnitude: strength of input (0 to 1)

        if (magnitude > 0.1f) // Dead zone to prevent drift
        {
            Vector3 movement;

            if (use3DMovement)
            {
                // 3D movement (XZ plane) - typical for 3D games
                movement = new Vector3(direction.x, 0, direction.y);
            }
            else
            {
                // 2D movement (XY plane) - typical for 2D games
                movement = new Vector3(direction.x, direction.y, 0);
            }

            // Move the object
            transform.position += movement * moveSpeed * magnitude * Time.deltaTime;

            // Rotate towards movement direction if enabled
            if (rotateTowardsDirection && movement.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    // Example: Enable/disable joystick based on game state
    public void EnableJoystick(bool enable)
    {
        if (joystick != null)
        {
            joystick.enabled = enable;
        }
    }

    // Example: Change joystick type at runtime
    public void SetFixedMode()
    {
        if (joystick != null)
        {
            joystick.SetJoystickType(JoystickType.Fixed);
        }
    }

    // Example: Change joystick type at runtime
    public void SetFloatingMode()
    {
        if (joystick != null)
        {
            joystick.SetJoystickType(JoystickType.Floating);
        }
    }

    // Example: Toggle fade effect
    public void ToggleFade(bool enabled)
    {
        if (joystick != null)
        {
            joystick.SetFadeEnabled(enabled);
        }
    }

    // Example: Toggle joystick visibility behavior
    public void ToggleHideWhenNotInUse(bool hide)
    {
        if (joystick != null)
        {
            joystick.SetHideWhenNotInUse(hide);
        }
    }

    // Example: Lock/unlock joystick input (useful for cutscenes, menus, etc.)
    public void LockJoystick()
    {
        if (joystick != null)
        {
            joystick.Lock();
            Debug.Log("Joystick input locked - can't move");
        }
    }

    public void UnlockJoystick()
    {
        if (joystick != null)
        {
            joystick.Unlock();
            Debug.Log("Joystick input unlocked - can move");
        }
    }

    public void ToggleLockInput(bool locked)
    {
        if (joystick != null)
        {
            joystick.LockInput(locked);
        }
    }

    // Example: Lock/unlock joystick background position
    public void LockJoystickPosition()
    {
        if (joystick != null)
        {
            joystick.LockBackgroundPosition();
            Debug.Log("Joystick background locked - won't move to new position");
        }
    }

    public void UnlockJoystickPosition()
    {
        if (joystick != null)
        {
            joystick.UnlockBackgroundPosition();
            Debug.Log("Joystick background unlocked - can move freely");
        }
    }

    public void ToggleLockBackground(bool locked)
    {
        if (joystick != null)
        {
            joystick.LockBackground(locked);
        }
    }

    // Example: Check if player is currently moving
    private void Update()
    {
        if (joystick != null && joystick.IsMoving)
        {
            // You can add logic here that runs only when the joystick is active
            // For example: play movement animation, show movement particles, etc.
        }
    }
}

