using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGames.UI
{
    /// <summary>
    /// Component that detects clicks/touches on a layer GameObject.
    /// Works with both mouse clicks (desktop) and touch input (mobile).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LayerClickDetector : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// Event fired when this layer is clicked/touched.
        /// Passes itself as the parameter so the manager knows which layer was clicked.
        /// </summary>
        public event Action<LayerClickDetector> OnClicked;

        /// <summary>
        /// Called by Unity's EventSystem when this object is clicked/touched.
        /// Works automatically for both mouse and touch input.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"LayerClickDetector: {gameObject.name} was clicked/touched");
            OnClicked?.Invoke(this);
        }

        private void OnValidate()
        {
            // Ensure there's a collider for the EventSystem to detect
            if (GetComponent<Collider2D>() == null)
            {
                Debug.LogWarning($"LayerClickDetector on {gameObject.name} requires a Collider2D component!");
            }
        }
    }
}


