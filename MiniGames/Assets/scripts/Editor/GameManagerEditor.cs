using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private string jsonFilePath = "";
    private string jsonText = "";
    private bool showJSONInput = false;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe from play mode state changes
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Handle play mode changes if needed
        // State changes: EnteredEditMode, ExitingEditMode, EnteredPlayMode, ExitingPlayMode
    }

    private static string DEFAULT_JSON = 
@"{
  ""levels"": [
    {
      ""levelId"": 1,
      ""mini_game_id"": ""1"",
      ""game_time"": 30,
      ""levelName"": ""Addition Basics"",
      ""questions"": [
        {
          ""statement"": ""_+5=6"",
          ""correctAnswer"": 1,
          ""options"": [1, 2, 3, 5],
          ""difficulty"": ""easy"",
          ""xp"": 10
        },
        {
          ""statement"": ""_+3=10"",
          ""correctAnswer"": 7,
          ""options"": [5, 6, 7, 8],
          ""difficulty"": ""easy"",
          ""xp"": 15
        },
        {
          ""statement"": ""4+_=9"",
          ""correctAnswer"": 5,
          ""options"": [3, 4, 5, 6],
          ""difficulty"": ""easy"",
          ""xp"": 20
        }
      ]
    },
    {
      ""levelId"": 2,
      ""mini_game_id"": ""1"",
      ""game_time"": 20,
      ""levelName"": ""Subtraction Challenge"",
      ""questions"": [
        {
          ""statement"": ""10-_=3"",
          ""correctAnswer"": 7,
          ""options"": [5, 6, 7, 8],
          ""difficulty"": ""medium"",
          ""xp"": 25
        },
        {
          ""statement"": ""_-5=8"",
          ""correctAnswer"": 13,
          ""options"": [11, 12, 13, 14],
          ""difficulty"": ""medium"",
          ""xp"": 30
        },
        {
          ""statement"": ""15-6=_"",
          ""correctAnswer"": 9,
          ""options"": [7, 8, 9, 10],
          ""difficulty"": ""medium"",
          ""xp"": 35
        }
      ]
    }
  ]
}";

    public override void OnInspectorGUI()
    {
        // Safety check
        if (target == null)
            return;

        // Update serialized object
        serializedObject.Update();

        // Draw default inspector
        DrawDefaultInspector();

        GameManager manager = (GameManager)target;
        
        if (manager == null)
            return;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use these tools to test the GameManager with JSON data.", MessageType.Info);

        EditorGUILayout.Space(10);

        // JSON Input Section
        showJSONInput = EditorGUILayout.Foldout(showJSONInput, "JSON Input", true);
        
        if (showJSONInput)
        {
            EditorGUI.indentLevel++;
            
            // Tab selection
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load from File", GUILayout.Height(25)))
            {
                LoadJSONFromFile();
            }
            if (GUILayout.Button("Use Default JSON", GUILayout.Height(25)))
            {
                jsonText = DEFAULT_JSON;
                jsonFilePath = "(Default Template)";
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // File path display
            if (!string.IsNullOrEmpty(jsonFilePath))
            {
                EditorGUILayout.LabelField("Current File:", jsonFilePath);
            }

            EditorGUILayout.Space(5);

            // JSON text area with scroll
            EditorGUILayout.LabelField("JSON Content:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            jsonText = EditorGUILayout.TextArea(jsonText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // Validation
        bool hasJSON = !string.IsNullOrEmpty(jsonText);
        bool isPlaying = Application.isPlaying;

        if (!isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test the game.", MessageType.Warning);
        }

        // Start Game Button
        EditorGUI.BeginDisabledGroup(!hasJSON || !isPlaying);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 14;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fixedHeight = 35;

        if (GUILayout.Button("▶ Start Game with JSON", buttonStyle))
        {
            Debug.Log("Attempting to start game...");
            Debug.Log($"JSON Length: {jsonText.Length} characters");
            
            if (ValidateJSON(jsonText))
            {
                manager.StartGame(jsonText);
                Debug.Log("Game started from Editor!");
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid JSON", 
                    "The JSON format is invalid. Please check the Console for details.\n\n" +
                    "Common issues:\n" +
                    "• Missing required fields (levelId, xp, mini_game_id, etc.)\n" +
                    "• Check for missing commas or brackets\n" +
                    "• Use 'Use Default JSON' button for a working template", 
                    "OK");
            }
        }
        
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // Quick Actions
        if (isPlaying)
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save JSON to File", GUILayout.Height(25)))
            {
                SaveJSONToFile();
            }
            
            if (GUILayout.Button("Clear JSON", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear JSON", 
                    "Are you sure you want to clear the JSON content?", 
                    "Yes", "No"))
                {
                    jsonText = "";
                    jsonFilePath = "";
                }
            }
            
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        // Info section
        EditorGUILayout.HelpBox(
            "JSON Format:\n" +
            "• levels: Array of level objects\n" +
            "• Each level needs: levelId, mini_game_id, game_time, levelName, questions\n" +
            "• Each question needs: statement, correctAnswer, options, difficulty, xp",
            MessageType.None);
    }

    private void LoadJSONFromFile()
    {
        string path = EditorUtility.OpenFilePanel("Select JSON File", Application.dataPath, "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                jsonText = File.ReadAllText(path);
                jsonFilePath = path;
                Debug.Log($"Loaded JSON from: {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to load JSON file:\n{e.Message}", 
                    "OK");
            }
        }
    }

    private void SaveJSONToFile()
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            EditorUtility.DisplayDialog("No JSON", 
                "There is no JSON content to save.", 
                "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save JSON File", Application.dataPath, "level_data", "json");
        
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                File.WriteAllText(path, jsonText);
                jsonFilePath = path;
                AssetDatabase.Refresh();
                Debug.Log($"Saved JSON to: {path}");
                EditorUtility.DisplayDialog("Success", 
                    "JSON file saved successfully!", 
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to save JSON file:\n{e.Message}", 
                    "OK");
            }
        }
    }

    private bool ValidateJSON(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("JSON Validation Error: JSON string is empty or null");
            return false;
        }

        try
        {
            // Try to parse with MiniJSON
            var jsonData = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
            
            if (jsonData == null)
            {
                Debug.LogError("JSON Validation Error: Failed to parse JSON - result is null");
                return false;
            }
            
            if (!jsonData.ContainsKey("levels"))
            {
                Debug.LogError("JSON Validation Error: No 'levels' key found in JSON root");
                return false;
            }
            
            var levelsList = jsonData["levels"] as List<object>;
            if (levelsList == null || levelsList.Count == 0)
            {
                Debug.LogError("JSON Validation Error: 'levels' is not a valid array or is empty");
                return false;
            }
            
            // Validate first level structure
            var firstLevel = levelsList[0] as Dictionary<string, object>;
            if (firstLevel == null)
            {
                Debug.LogError("JSON Validation Error: First level is not a valid object");
                return false;
            }
            
            // Check required fields in first level
            string[] requiredFields = { "levelId", "mini_game_id", "game_time", "levelName", "questions" };
            foreach (string field in requiredFields)
            {
                if (!firstLevel.ContainsKey(field))
                {
                    Debug.LogError($"JSON Validation Error: Missing required field '{field}' in level");
                    return false;
                }
            }
            
            // Check that questions array has content
            var questionsList = firstLevel["questions"] as List<object>;
            if (questionsList == null || questionsList.Count == 0)
            {
                Debug.LogError("JSON Validation Error: 'questions' array is empty or invalid");
                return false;
            }
            
            // Validate first question has xp field
            var firstQuestion = questionsList[0] as Dictionary<string, object>;
            if (firstQuestion != null && !firstQuestion.ContainsKey("xp"))
            {
                Debug.LogWarning("JSON Validation Warning: Question missing 'xp' field. Will use default value.");
            }
            
            Debug.Log($"✓ JSON Validated Successfully! Found {levelsList.Count} level(s)");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON Validation Error: {e.Message}");
            return false;
        }
    }
}

