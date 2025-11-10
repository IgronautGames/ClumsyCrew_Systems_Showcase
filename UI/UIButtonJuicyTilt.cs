using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Adds tactile "juice" to UI buttons using scale, rotation, and color feedback.
    /// 
    /// - Slightly scales and tilts button on press.
    /// - Highlights on hover.
    /// - Restores smoothly on release.
    /// - Supports multiple target images for complex UI compositions.
    /// </summary>
    public class UIButtonJuicyTilt : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Animation Settings")]
        [SerializeField] bool rotateTilt = true;
        [SerializeField] Color highlightColor = Color.yellow;

        [Header("Custom Highlight Targets")]
        [SerializeField] bool customImages;
        [SerializeField, ShowIf(nameof(customImages))] List<Image> customImagesToHighlight;

        Vector3 originalScale;
        Vector3 originalRotation;
        LTDescr scaleTween;
        LTDescr rotateTween;

        readonly List<Image> targetImages = new();
        readonly List<Color> originalColors = new();

        void Awake()
        {
            originalScale = transform.localScale;
            originalRotation = transform.localEulerAngles;

            // Determine which images to highlight
            if (customImages)
                targetImages = customImagesToHighlight;
            else
            {
                var mainImg = GetComponent<Image>() ?? GetComponentInChildren<Image>();
                if (mainImg != null)
                    targetImages.Add(mainImg);
            }

            foreach (var img in targetImages)
                if (img != null)
                    originalColors.Add(img.color);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CancelTweens();

            scaleTween = LeanTween.scale(gameObject, originalScale * 0.95f, 0.1f)
                .setEase(LeanTweenType.easeOutQuad);

            if (rotateTilt)
                rotateTween = LeanTween.rotateZ(gameObject, Random.Range(-8f, 8f), 0.1f)
                    .setEase(LeanTweenType.easeOutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelTweens();

            scaleTween = LeanTween.scale(gameObject, originalScale, 0.2f)
                .setEase(LeanTweenType.easeOutElastic);

            if (rotateTilt)
                rotateTween = LeanTween.rotate(gameObject, originalRotation, 0.4f)
                    .setEase(LeanTweenType.easeOutElastic);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            for (int i = 0; i < targetImages.Count; i++)
            {
                if (targetImages[i] == null) continue;

                Color newColor = highlightColor;
                newColor.a = originalColors[i].a;
                targetImages[i].color = newColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            for (int i = 0; i < targetImages.Count; i++)
                if (targetImages[i] != null)
                    targetImages[i].color = originalColors[i];
        }

        void CancelTweens()
        {
            if (LeanTween.isTweening(gameObject))
                LeanTween.cancel(gameObject);
        }
    }
}
