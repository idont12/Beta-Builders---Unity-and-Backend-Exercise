using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

public class MathStatementQuiz : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Transform characterContainer;
    [SerializeField] private Button[] optionButtons;

    [Header("Display Settings")]
    [SerializeField] private bool autoScaleToFit = true;
    [SerializeField] private float maxContainerWidth = 800f; // Max width for the statement
    [SerializeField] private float padding = 20f; // Padding on each side
    [SerializeField] private bool allowRetry = true; // Allow player to try again after wrong answer
    
    [Header("Visual Feedback")]
    [SerializeField] private Color wrongAnswerColor = Color.red; // Color for wrong answers
    [SerializeField] private Color correctAnswerColor = Color.green; // Color for correct answer

    // Events
    public event Action<int> OnStatementSolved;
    public event Action<int, int> OnWrongAnswer; // selected answer, correct answer

    private List<GameObject> spawnedCharacters = new List<GameObject>();
    private int correctAnswer;
    private string currentStatement;
    private bool isAnswered = false;
    private int underscoreCharacterIndex = -1; // Track which character GameObject is the underscore
    private Dictionary<Button, Color> originalButtonColors = new Dictionary<Button, Color>(); // Store original colors
    private Dictionary<Button, Image> buttonImages = new Dictionary<Button, Image>(); // Store button Image components

    /// <summary>
    /// Displays a new mathematical statement with answer options
    /// </summary>
    /// <param name="statement">The mathematical statement (e.g., "_+5=6")</param>
    /// <param name="options">Array of answer options to display on buttons</param>
    public void ShowStatement(string statement, int[] options)
    {
        Reset();
        
        currentStatement = statement;
        correctAnswer = CalculateCorrectAnswer(statement);
        
        // Create individual GameObjects for each character
        CreateCharacterGameObjects(statement);
        
        // Scale to fit if enabled
        if (autoScaleToFit)
        {
            ScaleStatementToFit();
        }
        
        // Setup option buttons
        SetupOptionButtons(options);
        
        isAnswered = false;
    }

    /// <summary>
    /// Resets the quiz state, destroying all spawned character GameObjects
    /// </summary>
    private void Reset()
    {
        // Destroy all spawned character GameObjects
        foreach (GameObject character in spawnedCharacters)
        {
            if (character != null)
            {
                Destroy(character);
            }
        }
        spawnedCharacters.Clear();
        
        // Reset container scale
        if (characterContainer != null)
        {
            characterContainer.localScale = Vector3.one;
        }
        
        // Reset button colors to original
        foreach (var kvp in buttonImages)
        {
            if (kvp.Key != null && kvp.Value != null && originalButtonColors.ContainsKey(kvp.Key))
            {
                kvp.Value.color = originalButtonColors[kvp.Key];
                kvp.Key.interactable = true;
            }
        }
        originalButtonColors.Clear();
        buttonImages.Clear();
        
        // Re-enable all buttons
        foreach (Button button in optionButtons)
        {
            if (button != null)
            {
                button.interactable = true;
            }
        }
        
        isAnswered = false;
    }

    /// <summary>
    /// Creates individual GameObjects for each token (numbers, operators, symbols) in the statement
    /// Multi-digit numbers are grouped together in one GameObject
    /// </summary>
    private void CreateCharacterGameObjects(string statement)
    {
        if (characterPrefab == null || characterContainer == null)
        {
            Debug.LogError("Character prefab or container is not assigned!");
            return;
        }

        underscoreCharacterIndex = -1;
        int tokenIndex = 0;

        // Parse statement into tokens (numbers and operators)
        List<string> tokens = ParseStatementIntoTokens(statement);

        foreach (string token in tokens)
        {
            GameObject charObj = Instantiate(characterPrefab, characterContainer);
            TextMeshProUGUI textComponent = charObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (textComponent != null)
            {
                textComponent.text = token;
                
                // Track which GameObject is the underscore
                if (token == "_")
                {
                    underscoreCharacterIndex = tokenIndex;
                }
            }
            else
            {
                Debug.LogError("Character prefab doesn't have a TextMeshProUGUI component!");
            }
            
            spawnedCharacters.Add(charObj);
            tokenIndex++;
        }
    }

    /// <summary>
    /// Parses the statement into tokens where multi-digit numbers are grouped together
    /// Example: "_+15=16" -> ["_", "+", "15", "=", "16"]
    /// </summary>
    private List<string> ParseStatementIntoTokens(string statement)
    {
        List<string> tokens = new List<string>();
        string currentNumber = "";

        for (int i = 0; i < statement.Length; i++)
        {
            char c = statement[i];

            // Skip spaces
            if (c == ' ')
            {
                if (!string.IsNullOrEmpty(currentNumber))
                {
                    tokens.Add(currentNumber);
                    currentNumber = "";
                }
                continue;
            }

            // If it's a digit, accumulate it
            if (char.IsDigit(c))
            {
                currentNumber += c;
            }
            else
            {
                // If we were building a number, add it first
                if (!string.IsNullOrEmpty(currentNumber))
                {
                    tokens.Add(currentNumber);
                    currentNumber = "";
                }

                // Add the operator or symbol as a separate token
                tokens.Add(c.ToString());
            }
        }

        // Don't forget the last number if the statement ends with one
        if (!string.IsNullOrEmpty(currentNumber))
        {
            tokens.Add(currentNumber);
        }

        return tokens;
    }

    /// <summary>
    /// Sets up the option buttons with the provided options
    /// </summary>
    private void SetupOptionButtons(int[] options)
    {
        if (optionButtons == null || optionButtons.Length == 0)
        {
            Debug.LogError("No option buttons assigned!");
            return;
        }

        int buttonCount = Mathf.Min(options.Length, optionButtons.Length);
        
        for (int i = 0; i < buttonCount; i++)
        {
            if (optionButtons[i] != null)
            {
                int optionValue = options[i];
                Button button = optionButtons[i];
                TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                
                if (buttonText != null)
                {
                    buttonText.text = optionValue.ToString();
                }
                
                // Get and store the button's Image component
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    if (!buttonImages.ContainsKey(button))
                    {
                        buttonImages[button] = buttonImage;
                    }
                    
                    // Store original button color
                    if (!originalButtonColors.ContainsKey(button))
                    {
                        originalButtonColors[button] = buttonImage.color;
                    }
                }
                else
                {
                    Debug.LogWarning($"Button {button.name} doesn't have an Image component!");
                }
                
                // Remove previous listeners and add new one
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnOptionSelected(optionValue, button));
                button.interactable = true;
            }
        }
    }

    /// <summary>
    /// Handles button click and validates the selected answer
    /// </summary>
    private void OnOptionSelected(int selectedAnswer, Button clickedButton)
    {
        if (isAnswered) return;
        
        // Validate and trigger appropriate event
        if (selectedAnswer == correctAnswer)
        {
            isAnswered = true;
            
            // Change the correct button to green
            SetButtonColor(clickedButton, correctAnswerColor);
            
            // Disable all buttons on correct answer
            foreach (Button button in optionButtons)
            {
                if (button != null)
                {
                    button.interactable = false;
                }
            }
            
            // Replace the underscore with the correct answer
            ReplaceUnderscoreWithAnswer(correctAnswer);
            
            OnStatementSolved?.Invoke(correctAnswer);
        }
        else
        {
            // Change the wrong button to red
            SetButtonColor(clickedButton, wrongAnswerColor);
            
            // Disable this specific button so it can't be clicked again
            clickedButton.interactable = false;
            
            // Wrong answer - trigger event but keep OTHER buttons enabled if allowRetry is true
            OnWrongAnswer?.Invoke(selectedAnswer, correctAnswer);
            
            if (!allowRetry)
            {
                isAnswered = true;
                
                // Disable all buttons
                foreach (Button button in optionButtons)
                {
                    if (button != null)
                    {
                        button.interactable = false;
                    }
                }
            }
            // If allowRetry is true, other buttons stay enabled and player can try again
        }
    }

    /// <summary>
    /// Sets the color of a button's Image component
    /// </summary>
    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        
        // Change the Image component color directly
        if (buttonImages.ContainsKey(button) && buttonImages[button] != null)
        {
            buttonImages[button].color = color;
        }
        else
        {
            // Fallback: try to get Image component
            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
            }
        }
    }

    /// <summary>
    /// Replaces the underscore character GameObject with the answer number
    /// </summary>
    private void ReplaceUnderscoreWithAnswer(int answer)
    {
        if (underscoreCharacterIndex >= 0 && underscoreCharacterIndex < spawnedCharacters.Count)
        {
            GameObject underscoreObj = spawnedCharacters[underscoreCharacterIndex];
            if (underscoreObj != null)
            {
                TextMeshProUGUI textComponent = underscoreObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = answer.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Scales the statement to fit within the parent container
    /// </summary>
    private void ScaleStatementToFit()
    {
        if (characterContainer == null || spawnedCharacters.Count == 0)
            return;

        // Wait for layout to update, then scale
        StartCoroutine(ScaleAfterLayout());
    }

    private System.Collections.IEnumerator ScaleAfterLayout()
    {
        // Wait for the layout to be calculated
        yield return new WaitForEndOfFrame();

        // Get the container's RectTransform
        RectTransform containerRect = characterContainer.GetComponent<RectTransform>();
        if (containerRect == null)
            yield break;

        // Calculate the total width of all characters
        float totalWidth = 0f;
        float maxHeight = 0f;

        foreach (GameObject charObj in spawnedCharacters)
        {
            RectTransform charRect = charObj.GetComponent<RectTransform>();
            if (charRect != null)
            {
                totalWidth += charRect.rect.width;
                maxHeight = Mathf.Max(maxHeight, charRect.rect.height);
            }
        }

        // Get the available width (use maxContainerWidth if set, otherwise use container width)
        float availableWidth = maxContainerWidth > 0 ? maxContainerWidth : containerRect.rect.width;
        availableWidth -= padding * 2; // Account for padding

        // Calculate scale factor needed to fit
        if (totalWidth > availableWidth)
        {
            float scaleFactor = availableWidth / totalWidth;
            
            // Apply scale to the container
            Vector3 currentScale = characterContainer.localScale;
            characterContainer.localScale = new Vector3(
                currentScale.x * scaleFactor,
                currentScale.y * scaleFactor,
                currentScale.z
            );
        }
    }

    /// <summary>
    /// Calculates the correct answer from the mathematical statement
    /// </summary>
    private int CalculateCorrectAnswer(string statement)
    {
        try
        {
            // Find the position of the underscore
            int underscoreIndex = statement.IndexOf('_');
            if (underscoreIndex == -1)
            {
                Debug.LogError("No underscore found in statement: " + statement);
                return 0;
            }

            // Find the equals sign
            int equalsIndex = statement.IndexOf('=');
            if (equalsIndex == -1)
            {
                Debug.LogError("No equals sign found in statement: " + statement);
                return 0;
            }

            string leftSide = statement.Substring(0, equalsIndex).Trim();
            string rightSide = statement.Substring(equalsIndex + 1).Trim();

            // Parse the equation based on where the underscore is
            if (rightSide.Contains("_"))
            {
                // Result is unknown: e.g., "5+3=_"
                return EvaluateExpression(leftSide);
            }
            else if (leftSide.StartsWith("_"))
            {
                // First operand is unknown: e.g., "_+5=6"
                return SolveForFirstOperand(leftSide, rightSide);
            }
            else if (leftSide.Contains("_"))
            {
                // Second operand is unknown: e.g., "5+_=6"
                return SolveForSecondOperand(leftSide, rightSide);
            }

            return 0;
        }
        catch (Exception e)
        {
            Debug.LogError("Error calculating answer: " + e.Message);
            return 0;
        }
    }

    /// <summary>
    /// Evaluates a simple mathematical expression
    /// </summary>
    private int EvaluateExpression(string expression)
    {
        return EvaluateExpressionAdvanced(expression);
    }

    /// <summary>
    /// Solves for the first operand (e.g., "_+5=6" returns 1, "_+5*2=12" returns 2)
    /// </summary>
    private int SolveForFirstOperand(string leftSide, string rightSide)
    {
        // Remove underscore and spaces
        leftSide = leftSide.Trim();
        int result = int.Parse(rightSide.Trim());
        
        // Find the first operator (the one directly after _)
        int operatorIndex = 1; // After the underscore
        char firstOp = leftSide[operatorIndex];
        
        // Get the rest of the expression after the first operator
        string remainingExpression = leftSide.Substring(operatorIndex + 1);
        
        // Evaluate the remaining expression with proper operator precedence
        int remainingValue = EvaluateExpressionAdvanced(remainingExpression);
        
        // Reverse the operation to solve for X
        switch (firstOp)
        {
            case '+': return result - remainingValue; // X + a = b -> X = b - a
            case '-': return result + remainingValue; // X - a = b -> X = b + a
            case '*': return result / remainingValue; // X * a = b -> X = b / a
            case '/': return result * remainingValue; // X / a = b -> X = b * a
            default: 
                Debug.LogError($"Unknown operator: {firstOp}");
                return 0;
        }
    }

    /// <summary>
    /// Solves for the second operand with multiple operations
    /// </summary>
    private int SolveForSecondOperand(string leftSide, string rightSide)
    {
        // For now, keep simple implementation for second operand
        // This handles cases like "5+_=6" or "10-_=3"
        leftSide = leftSide.Replace("_", "").Replace(" ", "");
        int result = int.Parse(rightSide.Trim());
        
        // Find the operator
        char op = FindOperator(leftSide);
        int operand1 = int.Parse(leftSide.Replace(op.ToString(), ""));
        
        // Reverse the operation to solve for second operand
        switch (op)
        {
            case '+': return result - operand1;
            case '-': return operand1 - result;
            case '*': return result / operand1;
            case '/': return operand1 / result;
            default: return 0;
        }
    }

    /// <summary>
    /// Advanced expression evaluator with operator precedence
    /// </summary>
    private int EvaluateExpressionAdvanced(string expression)
    {
        if (string.IsNullOrEmpty(expression)) return 0;
        
        expression = expression.Replace(" ", "");
        
        // Check if it's just a number
        if (int.TryParse(expression, out int simpleResult))
        {
            return simpleResult;
        }
        
        // Handle operator precedence: * and / before + and -
        // First, handle multiplication and division from left to right
        for (int i = 1; i < expression.Length; i++)
        {
            if (expression[i] == '*' || expression[i] == '/')
            {
                // Find the number before the operator
                int startIdx = i - 1;
                while (startIdx > 0 && char.IsDigit(expression[startIdx - 1]))
                {
                    startIdx--;
                }
                
                // Find the number after the operator
                int endIdx = i + 1;
                while (endIdx < expression.Length && char.IsDigit(expression[endIdx]))
                {
                    endIdx++;
                }
                
                int leftNum = int.Parse(expression.Substring(startIdx, i - startIdx));
                int rightNum = int.Parse(expression.Substring(i + 1, endIdx - i - 1));
                
                int result = expression[i] == '*' ? leftNum * rightNum : leftNum / rightNum;
                
                // Replace the operation with its result
                expression = expression.Substring(0, startIdx) + result + expression.Substring(endIdx);
                
                // Start over to handle all * and / 
                return EvaluateExpressionAdvanced(expression);
            }
        }
        
        // Now handle addition and subtraction from left to right
        for (int i = 1; i < expression.Length; i++)
        {
            if (expression[i] == '+' || expression[i] == '-')
            {
                int leftNum = int.Parse(expression.Substring(0, i));
                int rightNum = EvaluateExpressionAdvanced(expression.Substring(i + 1));
                
                return expression[i] == '+' ? leftNum + rightNum : leftNum - rightNum;
            }
        }
        
        return 0;
    }

    /// <summary>
    /// Finds the mathematical operator in the expression
    /// </summary>
    private char FindOperator(string expression)
    {
        if (expression.Contains("+")) return '+';
        if (expression.Contains("-")) return '-';
        if (expression.Contains("*")) return '*';
        if (expression.Contains("/")) return '/';
        return ' ';
    }

    /// <summary>
    /// Applies the mathematical operation
    /// </summary>
    private int ApplyOperation(int operand1, int operand2, char operation)
    {
        switch (operation)
        {
            case '+': return operand1 + operand2;
            case '-': return operand1 - operand2;
            case '*': return operand1 * operand2;
            case '/': return operand1 / operand2;
            default: return 0;
        }
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (optionButtons != null)
        {
            foreach (Button button in optionButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }
    }
}

