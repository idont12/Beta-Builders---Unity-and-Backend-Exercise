using System;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGames.UI
{
    /// <summary>
    /// Interface for Hidden Object Game implementations.
    /// Defines the contract for game management including events and lifecycle methods.
    /// </summary>
    public interface IMiniGame
    {
        // Events
        /// <summary>
        /// C# event fired when the player wins (finds the target object)
        /// </summary>
        event Action OnWin;

        /// <summary>
        /// UnityEvent for inspector configuration when player wins
        /// </summary>
        UnityEvent OnWinEvent { get; }

        // Properties
        /// <summary>
        /// Returns true if the game is currently active and playable
        /// </summary>
        bool IsGameActive { get; }

        // Methods
        /// <summary>
        /// Shows the game by hiding the cover and enabling gameplay
        /// </summary>
        void ShowGame();

        /// <summary>
        /// Hides the game by showing the cover and disabling gameplay
        /// </summary>
        void HideGame();

        /// <summary>
        /// Resets the game to initial state (positions) while keeping it active
        /// </summary>
        void ResetGame();
    }
}

