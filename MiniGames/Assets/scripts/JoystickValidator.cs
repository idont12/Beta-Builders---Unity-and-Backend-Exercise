using UnityEngine;
using MiniGames.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Validation script to verify joystick setup and test functionality.
/// Attach this alongside Joystick3D to validate the setup.
/// </summary>
[RequireComponent(typeof(Joystick3D))]
public class JoystickValidator : MonoBehaviour
{
    private Joystick3D joystick;
    private bool logMovement = false;

    [Header("Validation Results")]
    [SerializeField] private bool isSetupValid = false;
    [SerializeField] private string validationMessage = "Click 'Validate Setup' button";

    private void Awake()
    {
        joystick = GetComponent<Joystick3D>();
    }

    private void OnEnable()
    {
        if (joystick != null)
        {
            joystick.OnMovement += OnJoystickMovement;
        }
    }

    private void OnDisable()
    {
        if (joystick != null)
        {
            joystick.OnMovement -= OnJoystickMovement;
        }
    }

    private void OnJoystickMovement(Vector3 direction, float magnitude)
    {
        if (logMovement)
        {
            Debug.Log($"Joystick Input - Direction: ({direction.x:F2}, {direction.y:F2}), Magnitude: {magnitude:F2}");
        }
    }

    public void ValidateSetup()
    {
        validationMessage = "";
        isSetupValid = true;

        // Check for EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            validationMessage += "❌ No EventSystem found in scene! Input will not work.\n";
            isSetupValid = false;
        }
        else
        {
            validationMessage += "✅ EventSystem found\n";
        }

        // Check joystick component
        if (joystick == null)
        {
            validationMessage += "❌ Joystick3D component not found!\n";
            isSetupValid = false;
        }
        else
        {
            validationMessage += "✅ Joystick3D component attached\n";
        }

        // Check for Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            validationMessage += "❌ Joystick not under a Canvas!\n";
            isSetupValid = false;
        }
        else
        {
            validationMessage += $"✅ Canvas found (Mode: {canvas.renderMode})\n";
        }

        Debug.Log($"Joystick Validation:\n{validationMessage}");
        
        if (isSetupValid)
        {
            Debug.Log("🎮 Joystick setup is valid! Ready to use.");
        }
        else
        {
            Debug.LogWarning("⚠️ Joystick setup has issues. Please fix the errors above.");
        }
    }

    public void EnableMovementLogging(bool enable)
    {
        logMovement = enable;
        if (enable)
        {
            Debug.Log("Joystick movement logging enabled. Move the joystick to see input values.");
        }
        else
        {
            Debug.Log("Joystick movement logging disabled.");
        }
    }

    public void TestFadeEffect()
    {
        Debug.Log("Testing fade effect - watch the joystick opacity change");
        // The fade effect will be visible when you use and release the joystick
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(JoystickValidator))]
public class JoystickValidatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        JoystickValidator validator = (JoystickValidator)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate Setup", GUILayout.Height(30)))
        {
            validator.ValidateSetup();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable Movement Logging"))
        {
            validator.EnableMovementLogging(true);
        }
        if (GUILayout.Button("Disable Movement Logging"))
        {
            validator.EnableMovementLogging(false);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. Click 'Validate Setup' to check if joystick is configured correctly\n" +
            "2. Enter Play Mode\n" +
            "3. Enable Movement Logging to see input values\n" +
            "4. Test with mouse/touch input",
            MessageType.Info
        );
    }
}
#endif


