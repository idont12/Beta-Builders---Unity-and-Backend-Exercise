using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGames.UI
{
    /// <summary>
    /// Represents a single memory card in the game.
    /// Uses EXACT SAME pattern as LayerClickDetector for click detection.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class MemoryCard : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Event fired when this card is clicked/touched.
        /// Passes itself as the parameter so the manager knows which card was clicked.
        /// SAME PATTERN AS LayerClickDetector.OnClicked
        /// </summary>
        public event Action<MemoryCard> OnCardClicked;

        [Header("Visual Components")]
        [Tooltip("The SpriteRenderer that displays the card")]
        [SerializeField] private SpriteRenderer cardSpriteRenderer;
        
        [Tooltip("The sprite to show when card is face-down")]
        [SerializeField] private Sprite backSprite;

        [Header("Card State (Read Only)")]
        [Tooltip("Current card data assigned to this card")]
        [SerializeField] private CardData cardData;
        
        [Tooltip("Is this card currently revealed (face-up)?")]
        [SerializeField] private bool isRevealed = false;
        
        [Tooltip("Has this card been matched with its pair?")]
        [SerializeField] private bool isMatched = false;

        // Public Properties
        public bool IsRevealed => isRevealed;
        public bool IsMatched => isMatched;
        public int CurrentCardId => cardData?.cardId ?? -1;
        public CardData CardData => cardData;

        private void Awake()
        {
            // Auto-assign SpriteRenderer if not set
            if (cardSpriteRenderer == null)
            {
                cardSpriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            // Start with card face-down
            FlipToBack();
        }

        /// <summary>
        /// Called by Unity's EventSystem when this object is clicked/touched.
        /// Works automatically for both mouse and touch input.
        /// EXACT SAME AS LayerClickDetector.OnPointerClick
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[MemoryCard] {gameObject.name} was clicked/touched");
            
            // Don't allow clicking if already matched
            if (isMatched)
            {
                Debug.Log($"[MemoryCard] {gameObject.name} already matched - ignoring");
                return;
            }

            // Don't allow clicking if already revealed
            if (isRevealed)
            {
                Debug.Log($"[MemoryCard] {gameObject.name} already revealed - ignoring");
                return;
            }

            // Fire the event (same as LayerClickDetector)
            OnCardClicked?.Invoke(this);
        }

        /// <summary>
        /// Assigns card data and updates the visual state.
        /// Called by the game manager during randomization.
        /// </summary>
        public void SetCardData(CardData data)
        {
            cardData = data;
            
            // Reset state when new card is assigned
            isRevealed = false;
            isMatched = false;
            
            // Show back of card
            FlipToBack();
            
            Debug.Log($"[MemoryCard] '{gameObject.name}' assigned CardData with ID: {data?.cardId ?? -1}");
        }

        /// <summary>
        /// Flips the card to show its front face (reveal).
        /// </summary>
        public void FlipToFront()
        {
            if (cardData == null)
            {
                Debug.LogWarning($"Cannot flip card '{gameObject.name}' - no CardData assigned!");
                return;
            }

            isRevealed = true;
            
            if (cardSpriteRenderer != null && cardData.frontSprite != null)
            {
                cardSpriteRenderer.sprite = cardData.frontSprite;
                Debug.Log($"[MemoryCard] '{gameObject.name}' flipped to FRONT (ID: {cardData.cardId})");
            }
            else if (cardData.frontSprite == null)
            {
                Debug.LogWarning($"Card '{gameObject.name}' (ID: {cardData.cardId}) has no front sprite assigned!");
            }
        }

        /// <summary>
        /// Flips the card to show its back face (hide).
        /// </summary>
        public void FlipToBack()
        {
            isRevealed = false;
            
            if (cardSpriteRenderer != null && backSprite != null)
            {
                cardSpriteRenderer.sprite = backSprite;
            }
            else if (backSprite == null)
            {
                Debug.LogWarning($"Card '{gameObject.name}' has no back sprite assigned!");
            }
        }

        /// <summary>
        /// Marks this card as matched (permanently revealed).
        /// </summary>
        public void SetMatched()
        {
            isMatched = true;
            isRevealed = true;
            Debug.Log($"[MemoryCard] '{gameObject.name}' marked as MATCHED");
        }

        /// <summary>
        /// Resets the card to its initial state (face-down, not matched).
        /// </summary>
        public void ResetCard()
        {
            isMatched = false;
            isRevealed = false;
            FlipToBack();
        }

        private void OnValidate()
        {
            // Ensure there's a collider for the EventSystem to detect (same as LayerClickDetector)
            if (GetComponent<Collider2D>() == null)
            {
                Debug.LogWarning($"MemoryCard on {gameObject.name} requires a Collider2D component!");
            }
            
            // Auto-assign SpriteRenderer in editor
            if (cardSpriteRenderer == null)
            {
                cardSpriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }
}
