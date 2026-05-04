using UnityEngine;
using UnityEditor;

namespace MiniGames.UI.Editor
{
    [CustomEditor(typeof(HiddenObjectGameManager))]
    public class HiddenObjectGameManagerEditor : UnityEditor.Editor
    {
        private HiddenObjectGameManager manager;
        private bool isGameRunning = false;

        private void OnEnable()
        {
            manager = (HiddenObjectGameManager)target;
        }

        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            // Add spacing
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Testing Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use these buttons to test the game in Play Mode", MessageType.Info);

            // Check if we're in play mode
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the game", MessageType.Warning);
                GUI.enabled = false;
            }

            EditorGUILayout.BeginHorizontal();

            // Start Game button
            if (GUILayout.Button("Start Game", GUILayout.Height(40)))
            {
                if (manager != null)
                {
                    manager.ShowGame();
                    isGameRunning = true;
                    Debug.Log("Editor: Game Started");
                }
            }

            // Stop Game button
            if (GUILayout.Button("Stop Game", GUILayout.Height(40)))
            {
                if (manager != null)
                {
                    manager.HideGame();
                    isGameRunning = false;
                    Debug.Log("Editor: Game Stopped");
                }
            }

            EditorGUILayout.EndHorizontal();

            // Reset Game button (only enabled when game is running)
            if (Application.isPlaying && isGameRunning)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Reset Game", GUILayout.Height(30)))
                {
                    if (manager != null)
                    {
                        manager.ResetGame();
                        Debug.Log("Editor: Game Reset");
                    }
                }
            }

            // Re-enable GUI
            GUI.enabled = true;

            // Show game state
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Game State:", isGameRunning ? "Running" : "Stopped", EditorStyles.helpBox);
            }

            // Show target layer info
            EditorGUILayout.Space(5);
            SerializedProperty targetLayerProp = serializedObject.FindProperty("targetLayer");
            string targetLayerName = targetLayerProp.enumNames[targetLayerProp.enumValueIndex];
            EditorGUILayout.HelpBox($"🎯 Target to Win: {targetLayerName}", MessageType.Info);

            // Add helpful information
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Setup Checklist", EditorStyles.boldLabel);
            
            bool allValid = true;
            
            SerializedProperty frontLayer = serializedObject.FindProperty("frontLayerObject");
            SerializedProperty backLayer = serializedObject.FindProperty("backLayerObject");
            SerializedProperty joystick = serializedObject.FindProperty("joystick");
            SerializedProperty coverObject = serializedObject.FindProperty("coverObject");

            // Check Front Layer
            if (frontLayer.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠ Front Layer not assigned", MessageType.Error);
                allValid = false;
            }
            else
            {
                GameObject obj = frontLayer.objectReferenceValue as GameObject;
                if (obj != null)
                {
                    if (obj.GetComponent<Collider2D>() == null)
                    {
                        EditorGUILayout.HelpBox("⚠ Front Layer needs a Collider2D component", MessageType.Error);
                        allValid = false;
                    }
                    else if (obj.GetComponent<LayerClickDetector>() == null)
                    {
                        EditorGUILayout.HelpBox("ℹ Front Layer missing LayerClickDetector (will be added at runtime)", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("✓ Front Layer configured correctly", MessageType.None);
                    }
                }
            }

            // Check Back Layer
            if (backLayer.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠ Back Layer not assigned", MessageType.Error);
                allValid = false;
            }
            else
            {
                GameObject obj = backLayer.objectReferenceValue as GameObject;
                if (obj != null)
                {
                    if (obj.GetComponent<Collider2D>() == null)
                    {
                        EditorGUILayout.HelpBox("⚠ Back Layer needs a Collider2D component", MessageType.Error);
                        allValid = false;
                    }
                    else if (obj.GetComponent<LayerClickDetector>() == null)
                    {
                        EditorGUILayout.HelpBox("ℹ Back Layer missing LayerClickDetector (will be added at runtime)", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("✓ Back Layer configured correctly", MessageType.None);
                    }
                }
            }

            // Check Joystick
            if (joystick.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠ Joystick not assigned", MessageType.Error);
                allValid = false;
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Joystick assigned", MessageType.None);
            }

            // Check Cover Object (optional but recommended)
            if (coverObject.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("ℹ Cover Object not assigned (optional)", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Cover Object assigned", MessageType.None);
            }

            // Check for Physics2DRaycaster on camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
                {
                    EditorGUILayout.HelpBox("ℹ Main Camera missing Physics2DRaycaster (will be added at runtime)", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("✓ Camera has Physics2DRaycaster for click detection", MessageType.None);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("⚠ No Main Camera found in scene", MessageType.Warning);
            }

            // Overall status
            EditorGUILayout.Space(5);
            if (allValid)
            {
                EditorGUILayout.HelpBox("✓ All required components are properly configured!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠ Please configure all required components before testing", MessageType.Warning);
            }
        }
    }
}

