using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniGames.UI
{
    /// <summary>
    /// Diagnostic tool to check if EventSystem is working and receiving clicks.
    /// Attach this to any GameObject to see click debugging in the console.
    /// </summary>
    public class ClickDiagnostic : MonoBehaviour
    {
        private void Update()
        {
            // Check if EventSystem exists
            if (EventSystem.current == null)
            {
                Debug.LogError("❌ No EventSystem in scene!");
                return;
            }

            // Check for clicks
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"========== CLICK DIAGNOSTIC ==========");
                Debug.Log($"Mouse Position: {Input.mousePosition}");
                Debug.Log($"EventSystem exists: {EventSystem.current != null}");
                Debug.Log($"EventSystem enabled: {EventSystem.current?.enabled}");
                
                // Check what EventSystem thinks is being clicked
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;

                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                Debug.Log($"EventSystem raycast hits: {results.Count}");
                
                if (results.Count == 0)
                {
                    Debug.LogWarning("⚠️ EventSystem detected NO hits - raycaster might be missing!");
                }
                else
                {
                    foreach (var result in results)
                    {
                        Debug.Log($"  Hit: {result.gameObject.name} on layer {LayerMask.LayerToName(result.gameObject.layer)}");
                        Debug.Log($"       Has IPointerClickHandler: {result.gameObject.GetComponent<IPointerClickHandler>() != null}");
                    }
                }

                // Check raycasters
                var raycasters = FindObjectsOfType<BaseRaycaster>();
                Debug.Log($"Raycasters in scene: {raycasters.Length}");
                foreach (var raycaster in raycasters)
                {
                    Debug.Log($"  - {raycaster.GetType().Name} on {raycaster.gameObject.name} (enabled: {raycaster.enabled})");
                }

                Debug.Log($"======================================");
            }
        }
    }
}


