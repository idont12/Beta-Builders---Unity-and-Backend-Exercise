using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MiniGames.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#region Data Structures
[System.Serializable]
public class GameLevelList
{
    public List<LevelData> levels;
}

[System.Serializable]
public class LevelData
{
    public int levelId;
    public float xp;
    public string mini_game_id;
    public float game_time;
    public string levelName;
    public List<QuestionData> questions;
}

[System.Serializable]
public class QuestionData
{
    public string statement;
    public int correctAnswer;
    public int[] options;
    public string difficulty;
    public float xp; // XP for this specific question
}

[System.Serializable]
public class MiniGamePrefabMapping
{
    public string miniGameId;
    public GameObject prefab; // Must have IMiniGame component
}
#endregion

public class GameManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Mini-Game Setup")]
    [SerializeField] private List<MiniGamePrefabMapping> miniGamePrefabs;
    [SerializeField] private Transform miniGameContainer;
    [SerializeField] private MathStatementQuiz mathQuiz;
    
    [Header("UI References")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private MiniGameTimerDisplay timerDisplay;
    
    [Header("Win/Lose UI Text")]
    [SerializeField] private TextMeshProUGUI winXPText;
    [SerializeField] private TextMeshProUGUI loseMessageText;

    [Header("Backend (optional)")]
    [SerializeField] private MiniGames.Task.TaskGameBackendBridge backendBridge;
    #endregion

    #region Private Fields
    private LevelData currentLevel;
    private List<QuestionData> remainingQuestions;
    private QuestionData currentQuestion;
    private IMiniGame activeMiniGame;
    private GameObject activeMiniGameObject;
    private float currentXP;
    private float currentQuestionXP; // XP remaining for current question
    private float gameTimer;
    private bool isMiniGameActive;
    private Coroutine timerCoroutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Hide win/lose panels initially
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (mathQuiz != null)
        {
            mathQuiz.OnStatementSolved -= HandleCorrectAnswer;
            mathQuiz.OnWrongAnswer -= HandleWrongAnswer;
        }
        
        if (activeMiniGame != null)
        {
            activeMiniGame.OnWin -= OnMiniGameWin;
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Starts the game with the provided JSON level data
    /// </summary>
    /// <param name="jsonText">JSON string containing level data</param>
    public void StartGame(string jsonText)
    {
        try
        {
            // Parse JSON using MiniJSON
            var jsonData = MiniJSON.Json.Deserialize(jsonText) as Dictionary<string, object>;
            
            if (jsonData == null || !jsonData.ContainsKey("levels"))
            {
                Debug.LogError("Invalid JSON or no 'levels' key found!");
                return;
            }
            
            // Parse levels
            GameLevelList gameLevels = ParseGameLevelList(jsonData);
            
            if (gameLevels == null || gameLevels.levels == null || gameLevels.levels.Count == 0)
            {
                Debug.LogError("No valid levels found!");
                return;
            }
            
            // Load first level
            InitializeLevel(gameLevels.levels[0]);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Parses the JSON dictionary into GameLevelList object
    /// </summary>
    private GameLevelList ParseGameLevelList(Dictionary<string, object> jsonData)
    {
        GameLevelList result = new GameLevelList();
        result.levels = new List<LevelData>();
        
        var levelsList = jsonData["levels"] as List<object>;
        if (levelsList == null)
        {
            Debug.LogError("'levels' is not a valid array");
            return null;
        }
        
        foreach (var levelObj in levelsList)
        {
            var levelDict = levelObj as Dictionary<string, object>;
            if (levelDict != null)
            {
                LevelData level = ParseLevelData(levelDict);
                if (level != null)
                {
                    result.levels.Add(level);
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Parses a level dictionary into LevelData object
    /// </summary>
    private LevelData ParseLevelData(Dictionary<string, object> levelDict)
    {
        try
        {
            LevelData level = new LevelData();
            
            level.levelId = Convert.ToInt32(levelDict["levelId"]);
            
            // Level xp is now optional (backwards compatibility)
            if (levelDict.ContainsKey("xp"))
            {
                level.xp = Convert.ToSingle(levelDict["xp"]);
            }
            else
            {
                level.xp = 0; // Not used anymore with per-question XP
            }
            
            level.mini_game_id = levelDict["mini_game_id"] as string;
            level.game_time = Convert.ToSingle(levelDict["game_time"]);
            level.levelName = levelDict["levelName"] as string;
            
            level.questions = new List<QuestionData>();
            
            var questionsList = levelDict["questions"] as List<object>;
            if (questionsList != null)
            {
                foreach (var questionObj in questionsList)
                {
                    var questionDict = questionObj as Dictionary<string, object>;
                    if (questionDict != null)
                    {
                        QuestionData question = ParseQuestionData(questionDict);
                        if (question != null)
                        {
                            level.questions.Add(question);
                        }
                    }
                }
            }
            
            return level;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing level data: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parses a question dictionary into QuestionData object
    /// </summary>
    private QuestionData ParseQuestionData(Dictionary<string, object> questionDict)
    {
        try
        {
            QuestionData question = new QuestionData();
            
            question.statement = questionDict["statement"] as string;
            question.correctAnswer = Convert.ToInt32(questionDict["correctAnswer"]);
            question.difficulty = questionDict["difficulty"] as string;
            
            // Parse question XP
            if (questionDict.ContainsKey("xp"))
            {
                question.xp = Convert.ToSingle(questionDict["xp"]);
            }
            else
            {
                // Fallback: use default value if xp field is missing
                question.xp = 10;
                Debug.LogWarning($"Question '{question.statement}' missing 'xp' field, using default 10 XP");
            }
            
            // Parse options array
            var optionsList = questionDict["options"] as List<object>;
            if (optionsList != null)
            {
                question.options = new int[optionsList.Count];
                for (int i = 0; i < optionsList.Count; i++)
                {
                    question.options[i] = Convert.ToInt32(optionsList[i]);
                }
            }
            
            return question;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing question data: {e.Message}");
            return null;
        }
    }
    #endregion

    #region Level Management
    /// <summary>
    /// Initializes a level: sets up mini-game, resets XP, and shows first question
    /// </summary>
    private void InitializeLevel(LevelData level)
    {
        currentLevel = level;
        currentXP = level.xp;
        
        // Copy questions to remaining questions list
        remainingQuestions = new List<QuestionData>(level.questions);
        
        // Find and instantiate the correct mini-game prefab
        InstantiateMiniGame(level.mini_game_id);
        
        // Subscribe to math quiz events
        if (mathQuiz != null)
        {
            mathQuiz.OnStatementSolved -= HandleCorrectAnswer;
            mathQuiz.OnWrongAnswer -= HandleWrongAnswer;
            mathQuiz.OnStatementSolved += HandleCorrectAnswer;
            mathQuiz.OnWrongAnswer += HandleWrongAnswer;
        }
        else
        {
            Debug.LogError("MathStatementQuiz reference is missing!");
            return;
        }
        
        // Show first question
        ShowNextQuestion();
        
        Debug.Log($"Level '{level.levelName}' initialized with {remainingQuestions.Count} questions and {currentXP} XP");
    }

    /// <summary>
    /// Instantiates the mini-game prefab based on mini_game_id
    /// </summary>
    private void InstantiateMiniGame(string miniGameId)
    {
        // Clean up previous mini-game if exists
        if (activeMiniGameObject != null)
        {
            if (activeMiniGame != null)
            {
                activeMiniGame.OnWin -= OnMiniGameWin;
            }
            Destroy(activeMiniGameObject);
            activeMiniGame = null;
            activeMiniGameObject = null;
        }
        
        // Find the prefab mapping
        MiniGamePrefabMapping mapping = miniGamePrefabs.FirstOrDefault(m => m.miniGameId == miniGameId);
        
        if (mapping == null || mapping.prefab == null)
        {
            Debug.LogError($"No mini-game prefab found for ID: {miniGameId}");
            return;
        }
        
        // Instantiate the prefab
        Transform parent = miniGameContainer != null ? miniGameContainer : transform;
        activeMiniGameObject = Instantiate(mapping.prefab, parent);
        
        // Get the IMiniGame component
        activeMiniGame = activeMiniGameObject.GetComponent<IMiniGame>();
        
        if (activeMiniGame == null)
        {
            Debug.LogError($"Prefab for mini-game ID {miniGameId} doesn't have an IMiniGame component!");
            Destroy(activeMiniGameObject);
            activeMiniGameObject = null;
            return;
        }
        
        // Subscribe to win event
        activeMiniGame.OnWin += OnMiniGameWin;
        
        // Hide the mini-game initially
        activeMiniGame.HideGame();
        
        Debug.Log($"Mini-game instantiated for ID: {miniGameId}");
    }
    #endregion

    #region Question Management
    /// <summary>
    /// Shows the next question from the remaining questions list
    /// </summary>
    private void ShowNextQuestion()
    {
        if (remainingQuestions.Count == 0)
        {
            Debug.Log("No more questions available");
            EndGameLost();
            return;
        }
        
        // Get the first question
        currentQuestion = remainingQuestions[0];
        remainingQuestions.RemoveAt(0);
        
        // Initialize this question's XP pool
        currentQuestionXP = currentQuestion.xp;
        
        // Show the question on the math quiz
        if (mathQuiz != null)
        {
            mathQuiz.ShowStatement(currentQuestion.statement, currentQuestion.options);
            Debug.Log($"Showing question: {currentQuestion.statement} (XP: {currentQuestionXP})");
        }
    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// Handles correct answer from math quiz - starts the mini-game
    /// </summary>
    private void HandleCorrectAnswer(int answer)
    {
        Debug.Log($"Correct answer: {answer}");
        
        if (activeMiniGame == null)
        {
            Debug.LogError("No active mini-game to show!");
            return;
        }
        
        // Show the mini-game
        activeMiniGame.ShowGame();
        activeMiniGame.ResetGame();
        
        // Start the timer
        isMiniGameActive = true;
        gameTimer = currentLevel.game_time;
        
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(MiniGameTimerCoroutine());
    }

    /// <summary>
    /// Handles wrong answer from math quiz - deducts XP from current question's pool
    /// </summary>
    private void HandleWrongAnswer(int selectedAnswer, int correctAnswer)
    {
        if (currentQuestion == null)
        {
            Debug.LogError("No current question available!");
            return;
        }
        
        // Calculate XP loss from current question's XP pool
        float xpLoss = currentQuestion.xp / currentQuestion.options.Length;
        currentQuestionXP -= xpLoss;
        
        Debug.Log($"Wrong answer! Lost {xpLoss:F1} XP from this question. Question XP remaining: {currentQuestionXP:F1}");
        
        // Check if player still has questions to try
        // The player can keep trying the current question until all options are exhausted
        // But we move to next question or lose when no questions remain
    }

    /// <summary>
    /// Handles mini-game win event
    /// </summary>
    private void OnMiniGameWin()
    {
        Debug.Log("Mini-game won!");
        
        // Stop the timer
        isMiniGameActive = false;
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Hide timer display
        if (timerDisplay != null)
        {
            timerDisplay.HideTimer();
        }
        
        // Hide mini-game
        if (activeMiniGame != null)
        {
            activeMiniGame.HideGame();
        }
        
        // Show win panel with earned XP from this question
        EndGameWon(currentQuestionXP);
    }

    /// <summary>
    /// Called when mini-game timer expires
    /// </summary>
    private void OnMiniGameTimerExpired()
    {
        Debug.Log("Mini-game timer expired!");
        
        isMiniGameActive = false;
        
        // Hide timer display
        if (timerDisplay != null)
        {
            timerDisplay.HideTimer();
        }
        
        // Hide mini-game
        if (activeMiniGame != null)
        {
            activeMiniGame.HideGame();
        }
        
        // Move to next question or lose if no questions remain
        ShowNextQuestion();
    }
    #endregion

    #region Timer System
    /// <summary>
    /// Coroutine that counts down the mini-game timer
    /// </summary>
    private IEnumerator MiniGameTimerCoroutine()
    {
        // Show timer display
        if (timerDisplay != null)
        {
            timerDisplay.ShowTimer();
        }
        
        while (gameTimer > 0 && isMiniGameActive)
        {
            // Update timer display
            if (timerDisplay != null)
            {
                timerDisplay.UpdateTimer(gameTimer);
            }
            
            yield return null;
            gameTimer -= Time.deltaTime;
        }
        
        // Timer expired
        if (isMiniGameActive)
        {
            OnMiniGameTimerExpired();
        }
    }
    #endregion

    #region Win/Lose Conditions
    /// <summary>
    /// Ends the game with a win - shows XP earned
    /// </summary>
    private void EndGameWon(float earnedXP)
    {
        Debug.Log($"Game Won! XP Earned: {earnedXP:F1}");
        
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            
            if (winXPText != null)
            {
                winXPText.text = $"XP Earned: {Mathf.RoundToInt(earnedXP)}";
            }
        }

        if (backendBridge != null)
            backendBridge.ReportMiniGameWin(Mathf.RoundToInt(earnedXP));
    }

    /// <summary>
    /// Ends the game with a loss - shows game over
    /// </summary>
    private void EndGameLost()
    {
        Debug.Log("Game Lost!");
        
        if (losePanel != null)
        {
            losePanel.SetActive(true);
            
            if (loseMessageText != null)
            {
                loseMessageText.text = "Game Over! You ran out of questions.";
            }
        }
    }
    #endregion

    #region Editor Testing
    [ContextMenu("Test JSON Load")]
    private void TestJSONLoad()
    {
        string testJSON = @"{
            ""levels"": [
                {
                    ""levelId"": 1,
                    ""xp"": 20,
                    ""mini_game_id"": ""1"",
                    ""game_time"": 30,
                    ""levelName"": ""Addition Basics"",
                    ""questions"": [
                        {
                            ""statement"": ""_+5=6"",
                            ""correctAnswer"": 1,
                            ""options"": [1, 2, 3, 5],
                            ""difficulty"": ""easy""
                        },
                        {
                            ""statement"": ""_+3=10"",
                            ""correctAnswer"": 7,
                            ""options"": [5, 6, 7, 8],
                            ""difficulty"": ""easy""
                        }
                    ]
                }
            ]
        }";
        
        StartGame(testJSON);
    }
    #endregion
}
