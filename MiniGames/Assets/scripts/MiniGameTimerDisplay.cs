using UnityEngine;
using TMPro;

/// <summary>
/// Displays a countdown timer in MM:SS format with color warning when time is low
/// </summary>
public class MiniGameTimerDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject timerContainer;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private float warningThreshold = 10f;

    private void Awake()
    {
        // Hide timer initially
        HideTimer();
        
        // Set default color
        if (timerText != null)
        {
            timerText.color = normalColor;
        }
    }

    /// <summary>
    /// Makes the timer visible
    /// </summary>
    public void ShowTimer()
    {
        if (timerContainer != null)
        {
            timerContainer.SetActive(true);
        }
        else if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hides the timer
    /// </summary>
    public void HideTimer()
    {
        if (timerContainer != null)
        {
            timerContainer.SetActive(false);
        }
        else if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the timer display with MM:SS format and appropriate color
    /// </summary>
    /// <param name="timeRemaining">Time remaining in seconds</param>
    public void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;
        
        // Ensure time doesn't go negative
        timeRemaining = Mathf.Max(0, timeRemaining);
        
        // Convert to MM:SS format
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        
        // Update color based on threshold
        if (timeRemaining <= warningThreshold)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }

    /// <summary>
    /// Resets the timer to default state
    /// </summary>
    public void ResetTimer()
    {
        if (timerText != null)
        {
            timerText.text = "00:00";
            timerText.color = normalColor;
        }
        
        HideTimer();
    }
}

