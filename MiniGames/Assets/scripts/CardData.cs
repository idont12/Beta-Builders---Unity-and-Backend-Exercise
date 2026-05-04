using System;
using UnityEngine;

namespace MiniGames.UI
{
    /// <summary>
    /// Serializable class that holds data for a single memory card type.
    /// Define multiple card types directly in the MemoryCardGameManager inspector.
    /// </summary>
    [Serializable]
    public class CardData
    {
        [Tooltip("Unique identifier for this card type. Cards with the same ID are considered a match.")]
        public int cardId;
        
        [Tooltip("The sprite shown when the card is flipped face-up")]
        public Sprite frontSprite;
        
        /// <summary>
        /// Creates a new CardData with the specified ID and sprite
        /// </summary>
        public CardData(int id, Sprite sprite)
        {
            cardId = id;
            frontSprite = sprite;
        }
        
        /// <summary>
        /// Default constructor for Unity serialization
        /// </summary>
        public CardData()
        {
            cardId = 0;
            frontSprite = null;
        }
    }
}

