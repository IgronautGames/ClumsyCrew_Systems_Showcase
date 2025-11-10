using UnityEngine;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Automatically adjusts UI elements to fit within the device's safe area.
    /// 
    /// Prevents content from being clipped by notches or rounded screen edges.
    /// 
    /// Usage:
    /// - Attach to a Canvas GameObject.
    /// - Assign UI RectTransforms to <see cref="safeScreenObjects"/>.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class SafeScreen : MonoBehaviour
    {
        [Tooltip("UI RectTransforms that should respect the device safe area.")]
        public RectTransform[] safeScreenObjects;

        private void Awake()
        {
            Canvas canvas = GetComponent<Canvas>();
            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            // Normalize to canvas dimensions
            anchorMin.x /= canvas.pixelRect.width;
            anchorMin.y /= canvas.pixelRect.height;
            anchorMax.x /= canvas.pixelRect.width;
            anchorMax.y /= canvas.pixelRect.height;

            // Apply safe area to all UI objects
            foreach (RectTransform rect in safeScreenObjects)
            {
                if (rect == null) continue;
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
            }

#if UNITY_EDITOR
            Debug.Log($"✅ Safe area applied ({safeArea.width}x{safeArea.height})");
#endif
        }
    }
}
