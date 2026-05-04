using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MathStatementQuiz))]
public class MathStatementQuizEditor : Editor
{
    private string testStatement = "_+5=6";
    private int[] testOptions = new int[] { 1, 2, 3, 5 };
    private SerializedProperty testOptionsProperty;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        MathStatementQuiz quiz = (MathStatementQuiz)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use these fields to test the math statement quiz without writing additional scripts.", MessageType.Info);

        EditorGUILayout.Space(10);

        // Test statement field
        EditorGUILayout.LabelField("Mathematical Statement", EditorStyles.label);
        testStatement = EditorGUILayout.TextField(testStatement);
        EditorGUILayout.HelpBox("Examples: \"_+5=6\", \"10-_=3\", \"2*4=_\"", MessageType.None);

        EditorGUILayout.Space(10);

        // Test options field
        EditorGUILayout.LabelField("Answer Options", EditorStyles.label);
        
        // Create a resizable array field
        int newSize = EditorGUILayout.IntField("Number of Options", testOptions.Length);
        if (newSize != testOptions.Length && newSize >= 2)
        {
            System.Array.Resize(ref testOptions, newSize);
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < testOptions.Length; i++)
        {
            testOptions[i] = EditorGUILayout.IntField($"Option {i + 1}", testOptions[i]);
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);

        // Show Statement button
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Show Statement", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                quiz.ShowStatement(testStatement, testOptions);
                Debug.Log($"Showing statement: {testStatement} with options: [{string.Join(", ", testOptions)}]");
            }
        }
        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test the Show Statement button.", MessageType.Warning);
        }

        EditorGUILayout.Space(10);

        // Quick presets
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Addition (_+5=6)"))
        {
            testStatement = "_+5=6";
            testOptions = new int[] { 1, 2, 3, 5 };
        }
        if (GUILayout.Button("Subtraction (10-_=3)"))
        {
            testStatement = "10-_=3";
            testOptions = new int[] { 5, 6, 7, 8 };
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Multiplication (2*_=8)"))
        {
            testStatement = "2*_=8";
            testOptions = new int[] { 2, 4, 6, 8 };
        }
        if (GUILayout.Button("Result (5+3=_)"))
        {
            testStatement = "5+3=_";
            testOptions = new int[] { 6, 7, 8, 9 };
        }
        EditorGUILayout.EndHorizontal();
    }
}


