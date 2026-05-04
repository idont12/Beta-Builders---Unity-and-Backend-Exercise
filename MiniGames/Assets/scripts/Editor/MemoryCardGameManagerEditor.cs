using UnityEngine;
using UnityEditor;

namespace MiniGames.UI.Editor
{
    /// <summary>
    /// Custom editor for MemoryCardGameManager.
    /// Provides runtime testing controls and setup validation in the inspector.
    /// </summary>
    [CustomEditor(typeof(MemoryCardGameManager))]
    public class MemoryCardGameManagerEditor : UnityEditor.Editor
    {
        private MemoryCardGameManager manager;
        private bool isGameRunning = false;

        private void OnEnable()
        {
            manager = (MemoryCardGameManager)target;
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
                    Debug.Log("Editor: Memory Game Started");
                }
            }

            // Stop Game button
            if (GUILayout.Button("Stop Game", GUILayout.Height(40)))
            {
                if (manager != null)
                {
                    manager.HideGame();
                    isGameRunning = false;
                    Debug.Log("Editor: Memory Game Stopped");
                }
            }

            EditorGUILayout.EndHorizontal();

            // Reset Game button (only enabled when game is running)
            if (Application.isPlaying && isGameRunning)
            {
                EditorGUILayout.Space(5);
                if (GUILayout.Button("Reset Game (New Random Cards)", GUILayout.Height(30)))
                {
                    if (manager != null)
                    {
                        manager.ResetGame();
                        Debug.Log("Editor: Memory Game Reset with new random cards");
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

            // Quick setup guide
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Setup Guide", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Import card front sprites into your project\n" +
                "2. Place card GameObjects in scene with SpriteRenderer and BoxCollider2D\n" +
                "3. Add MemoryCard script to each card GameObject\n" +
                "4. Assign all cards to the 'Cards' array above\n" +
                "5. In 'Card Pool' array:\n" +
                "   - Set size (e.g., 15 for variety)\n" +
                "   - For each entry: set Card Id (unique) and Front Sprite\n" +
                "6. Assign a back sprite (same for all cards)\n" +
                "7. Optionally assign a cover GameObject\n" +
                "8. Press Play and click 'Start Game' to test!",
                MessageType.None
            );
        }
    }
}

