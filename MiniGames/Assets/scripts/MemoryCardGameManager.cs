using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGames.UI
{
    /// <summary>
    /// Memory card matching game manager that implements the IMiniGame interface.
    /// Players flip cards to find matching pairs. Cards are randomly selected from a pool
    /// and randomly placed on the board each game.
    /// </summary>
    public class MemoryCardGameManager : MonoBehaviour, IMiniGame
    {
        // Public Events (Interface Implementation)
        public event Action OnWin;
        
        // Public Properties (Interface Implementation)
        public bool IsGameActive => isGameActive;
        public UnityEvent OnWinEvent => _onWinEvent;

        [Header("Events")]
        [Tooltip("Unity Event fired when player wins (for inspector configuration)")]
        [SerializeField] private UnityEvent _onWinEvent;

        [Header("Card Setup")]
        [Tooltip("All card GameObjects in the scene (manually placed in desired positions)")]
        [SerializeField] private MemoryCard[] cards;
        
        [Tooltip("Pool of available card types to randomly select from. Add card IDs and sprites here.")]
        [SerializeField] private CardData[] cardPool;
        
        [Tooltip("Universal back sprite for all cards")]
        [SerializeField] private Sprite backSprite;

        [Header("Game Settings")]
        [Tooltip("Cover object that hides/shows the game")]
        [SerializeField] private GameObject coverObject;
        
        [Tooltip("Time to wait before hiding non-matching cards (in seconds)")]
        [SerializeField] private float mismatchHideDelay = 1f;

        // Private game state
        private bool isGameActive = false;
        private MemoryCard firstCard = null;
        private MemoryCard secondCard = null;
        private int matchedPairs = 0;
        private int totalPairs = 0;
        private bool isCheckingMatch = false;
        private Coroutine hideCardsCoroutine = null;

        private void Awake()
        {
            Debug.Log("========== MemoryCardGameManager Awake ==========");
            
            // Validate setup
            if (!ValidateSetup())
            {
                enabled = false;
                return;
            }

            // Ensure EventSystem exists for click detection (same as HiddenObjectGameManager)
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("✓ MemoryCardGameManager: Created EventSystem for card click detection");
            }
            else
            {
                Debug.Log("✓ MemoryCardGameManager: EventSystem already exists");
            }

            // Ensure camera has Physics2DRaycaster for 2D click detection (same as HiddenObjectGameManager)
            Camera cam = Camera.main;
            if (cam != null)
            {
                var raycaster = cam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
                if (raycaster == null)
                {
                    raycaster = cam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
                    Debug.Log("✓ MemoryCardGameManager: Added Physics2DRaycaster to main camera");
                }
                else
                {
                    Debug.Log("✓ MemoryCardGameManager: Physics2DRaycaster already on camera");
                }
                
                // Log raycaster info
                Debug.Log($"   Camera: {cam.name}");
                Debug.Log($"   Event Mask: {raycaster.eventMask.value}");
                Debug.Log($"   Max Ray Intersections: {raycaster.maxRayIntersections}");
            }
            else
            {
                Debug.LogError("❌ MemoryCardGameManager: No Main Camera found!");
            }
            
            Debug.Log("================================================");
        }

        private void Start()
        {
            // Start with game hidden if cover is assigned
            if (coverObject != null)
            {
                coverObject.SetActive(true);
            }
        }

        /// <summary>
        /// Validates that all required components are properly configured
        /// </summary>
        private bool ValidateSetup()
        {
            bool isValid = true;

            if (cards == null || cards.Length == 0)
            {
                Debug.LogError("MemoryCardGameManager: No cards assigned!");
                isValid = false;
            }
            else if (cards.Length % 2 != 0)
            {
                Debug.LogError($"MemoryCardGameManager: Card count must be even! Currently: {cards.Length}");
                isValid = false;
            }

            if (cardPool == null || cardPool.Length == 0)
            {
                Debug.LogError("MemoryCardGameManager: Card pool is empty!");
                isValid = false;
            }
            else
            {
                int pairsNeeded = cards.Length / 2;
                if (cardPool.Length < pairsNeeded)
                {
                    Debug.LogError($"MemoryCardGameManager: Card pool has {cardPool.Length} cards but needs at least {pairsNeeded} for {cards.Length} card positions!");
                    isValid = false;
                }
            }

            if (backSprite == null)
            {
                Debug.LogWarning("MemoryCardGameManager: Back sprite not assigned!");
            }

            return isValid;
        }

        /// <summary>
        /// Shows the game by hiding the cover and enabling gameplay.
        /// Randomizes cards and subscribes to events.
        /// </summary>
        public void ShowGame()
        {
            if (!enabled) return;

            Debug.Log("MemoryCardGameManager: ShowGame() called");

            // Reset game state
            matchedPairs = 0;
            totalPairs = cards.Length / 2;
            firstCard = null;
            secondCard = null;
            isCheckingMatch = false;

            // Stop any running coroutines
            if (hideCardsCoroutine != null)
            {
                StopCoroutine(hideCardsCoroutine);
                hideCardsCoroutine = null;
            }

            // Randomize cards
            RandomizeCards();

            // Hide the cover
            if (coverObject != null)
            {
                coverObject.SetActive(false);
            }

            // Subscribe to card click events
            foreach (MemoryCard card in cards)
            {
                if (card != null)
                {
                    card.OnCardClicked += OnCardClicked;
                }
            }

            isGameActive = true;
            Debug.Log($"MemoryCardGameManager: Game started! Total pairs: {totalPairs}");
        }

        /// <summary>
        /// Hides the game by showing the cover and disabling gameplay.
        /// Unsubscribes from events and resets card states.
        /// </summary>
        public void HideGame()
        {
            Debug.Log("MemoryCardGameManager: HideGame() called");

            // Show the cover
            if (coverObject != null)
            {
                coverObject.SetActive(true);
            }

            // Unsubscribe from card click events
            foreach (MemoryCard card in cards)
            {
                if (card != null)
                {
                    card.OnCardClicked -= OnCardClicked;
                }
            }

            // Stop any running coroutines
            if (hideCardsCoroutine != null)
            {
                StopCoroutine(hideCardsCoroutine);
                hideCardsCoroutine = null;
            }

            // Reset all cards
            foreach (MemoryCard card in cards)
            {
                if (card != null)
                {
                    card.ResetCard();
                }
            }

            // Reset game state
            firstCard = null;
            secondCard = null;
            matchedPairs = 0;
            isCheckingMatch = false;
            isGameActive = false;

            Debug.Log("MemoryCardGameManager: Game hidden!");
        }

        /// <summary>
        /// Resets the game to initial state while keeping it active.
        /// Re-randomizes cards and resets progress.
        /// </summary>
        public void ResetGame()
        {
            if (!isGameActive)
            {
                Debug.LogWarning("MemoryCardGameManager: Cannot reset - game is not active!");
                return;
            }

            Debug.Log("MemoryCardGameManager: ResetGame() called");

            // Stop any running coroutines
            if (hideCardsCoroutine != null)
            {
                StopCoroutine(hideCardsCoroutine);
                hideCardsCoroutine = null;
            }

            // Reset game state
            matchedPairs = 0;
            firstCard = null;
            secondCard = null;
            isCheckingMatch = false;

            // Randomize cards again (new game)
            RandomizeCards();

            Debug.Log($"MemoryCardGameManager: Game reset! New random layout with {totalPairs} pairs.");
        }

        /// <summary>
        /// Randomly selects cards from the pool and shuffles them across board positions.
        /// </summary>
        private void RandomizeCards()
        {
            int pairsNeeded = cards.Length / 2;

            // Validate we have enough cards in the pool
            if (cardPool.Length < pairsNeeded)
            {
                Debug.LogError($"Cannot randomize: pool has {cardPool.Length} cards but need {pairsNeeded}!");
                return;
            }

            // Step 1: Randomly select N unique CardData from the pool
            List<CardData> selectedCards = SelectRandomCards(pairsNeeded);

            // Step 2: Create pairs by duplicating each selected card
            List<CardData> cardPairs = new List<CardData>();
            foreach (CardData cardData in selectedCards)
            {
                cardPairs.Add(cardData);
                cardPairs.Add(cardData); // Add twice for pair
            }

            // Step 3: Shuffle the pairs using Fisher-Yates algorithm
            ShuffleList(cardPairs);

            // Step 4: Assign shuffled cards to board positions
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && i < cardPairs.Count)
                {
                    cards[i].SetCardData(cardPairs[i]);
                    Debug.Log($"Card {i} assigned: ID={cardPairs[i].cardId}, Sprite={cardPairs[i].frontSprite?.name}");
                }
            }

            Debug.Log($"Randomization complete: {pairsNeeded} pairs from pool of {cardPool.Length} cards");
        }

        /// <summary>
        /// Selects N random unique CardData objects from the card pool.
        /// </summary>
        private List<CardData> SelectRandomCards(int count)
        {
            // Create a list of indices
            List<int> indices = new List<int>();
            for (int i = 0; i < cardPool.Length; i++)
            {
                indices.Add(i);
            }

            // Shuffle indices
            ShuffleList(indices);

            // Take the first N cards
            List<CardData> selected = new List<CardData>();
            for (int i = 0; i < count && i < indices.Count; i++)
            {
                selected.Add(cardPool[indices[i]]);
            }

            return selected;
        }

        /// <summary>
        /// Shuffles a list using Fisher-Yates algorithm.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Handles card click events from MemoryCard components.
        /// </summary>
        private void OnCardClicked(MemoryCard clickedCard)
        {
            // Ignore clicks if game is not active or currently checking a match
            if (!isGameActive || isCheckingMatch)
            {
                Debug.Log("Card click ignored - game not active or checking match");
                return;
            }

            // Ignore if card is already matched or revealed
            if (clickedCard.IsMatched || clickedCard.IsRevealed)
            {
                Debug.Log("Card click ignored - already matched or revealed");
                return;
            }

            // Flip the card
            clickedCard.FlipToFront();

            // Handle based on current state
            if (firstCard == null)
            {
                // First card in pair
                firstCard = clickedCard;
                Debug.Log($"First card selected: ID={firstCard.CurrentCardId}");
            }
            else if (secondCard == null && clickedCard != firstCard)
            {
                // Second card in pair
                secondCard = clickedCard;
                Debug.Log($"Second card selected: ID={secondCard.CurrentCardId}");

                // Check for match after a brief moment
                isCheckingMatch = true;
                hideCardsCoroutine = StartCoroutine(CheckForMatchAfterDelay());
            }
        }

        /// <summary>
        /// Coroutine that checks if two flipped cards match after a brief delay.
        /// </summary>
        private IEnumerator CheckForMatchAfterDelay()
        {
            // Small delay to let player see both cards
            yield return new WaitForSeconds(0.5f);

            CheckForMatch();
        }

        /// <summary>
        /// Checks if the two currently selected cards match.
        /// </summary>
        private void CheckForMatch()
        {
            if (firstCard == null || secondCard == null)
            {
                isCheckingMatch = false;
                return;
            }

            // Compare card IDs
            if (firstCard.CurrentCardId == secondCard.CurrentCardId)
            {
                // Match found!
                Debug.Log($"✓ MATCH! Card ID: {firstCard.CurrentCardId}");

                firstCard.SetMatched();
                secondCard.SetMatched();
                matchedPairs++;

                Debug.Log($"Matched pairs: {matchedPairs}/{totalPairs}");

                // Check for win condition
                if (matchedPairs >= totalPairs)
                {
                    Debug.Log("🎉 ALL PAIRS MATCHED - PLAYER WINS!");
                    OnWin?.Invoke();
                    _onWinEvent?.Invoke();
                }

                // Reset selection immediately for matches
                firstCard = null;
                secondCard = null;
                isCheckingMatch = false;
            }
            else
            {
                // No match - hide cards after delay
                Debug.Log($"✗ No match: {firstCard.CurrentCardId} ≠ {secondCard.CurrentCardId}");
                hideCardsCoroutine = StartCoroutine(HideCardsAfterDelay());
            }
        }

        /// <summary>
        /// Coroutine that hides non-matching cards after a delay.
        /// </summary>
        private IEnumerator HideCardsAfterDelay()
        {
            // Wait for configured delay
            yield return new WaitForSeconds(mismatchHideDelay);

            // Flip cards back
            if (firstCard != null)
            {
                firstCard.FlipToBack();
            }
            if (secondCard != null)
            {
                secondCard.FlipToBack();
            }

            // Reset selection
            firstCard = null;
            secondCard = null;
            isCheckingMatch = false;
            hideCardsCoroutine = null;

            Debug.Log("Cards hidden - ready for next selection");
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (cards != null)
            {
                foreach (MemoryCard card in cards)
                {
                    if (card != null)
                    {
                        card.OnCardClicked -= OnCardClicked;
                    }
                }
            }
        }

        private void OnDisable()
        {
            // Ensure we unsubscribe if disabled
            if (cards != null)
            {
                foreach (MemoryCard card in cards)
                {
                    if (card != null)
                    {
                        card.OnCardClicked -= OnCardClicked;
                    }
                }
            }

            // Stop any running coroutines
            if (hideCardsCoroutine != null)
            {
                StopCoroutine(hideCardsCoroutine);
                hideCardsCoroutine = null;
            }
        }

        // Gizmos for visualizing card positions in editor
        private void OnDrawGizmos()
        {
            if (cards == null || cards.Length == 0)
                return;

            Gizmos.color = Color.cyan;

            // Draw lines connecting cards to show the grid
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                {
                    Vector3 pos = cards[i].transform.position;
                    Gizmos.DrawWireSphere(pos, 0.1f);
                }
            }
        }
    }
}

