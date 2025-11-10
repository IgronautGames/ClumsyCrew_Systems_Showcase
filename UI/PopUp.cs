using ClumsyCrew.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Base popup class that supports animated open/close, blur effects, and background fitting.
    /// - Used by <see cref="PopupManager"/> for centralized popup lifecycle management.
    /// - Handles UI sound feedback and optional animator-based transitions.
    /// - Can run in "small" mode (LeanTween animation) or Animator-driven mode.
    /// </summary>
    public class PopUp : MonoBehaviour
    {
        [Header("General Settings")]
        [SerializeField] bool smallPopUp;
        [SerializeField] protected Button closeButton;
        [SerializeField] protected GameObject content;
        [SerializeField] bool ignoreClosingOnAnotherPopUpOpen;
        [SerializeField] bool closeOtherPopUpsOnOpeningThis = true;

        [Header("Animation Settings")]
        [SerializeField] bool haveAnimation;
        [ShowIf("haveAnimation")][SerializeField] bool haveAnimator;
        [HideIf("haveAnimator")][SerializeField] bool smallPopUpAnim;
        [ShowIf("smallPopUpAnim")][SerializeField] GameObject parentToAnimate;
        [ShowIf("smallPopUpAnim")][SerializeField] Image dimmedBackground;
        [ShowIf("haveAnimator")][SerializeField] float closingAnimLenght;
        [ShowIf("haveAnimator")][SerializeField] protected Animator anim;

        [Header("Sound")]
        [SerializeField] bool uniqueSounds;
        [ShowIf("uniqueSounds")][SerializeField] AudioClipData openingSound;
        [ShowIf("uniqueSounds")][SerializeField] AudioClipData closingSound;

        [Header("Background Scaling")]
        [SerializeField] bool resizeBgToFitTheScreen;
        [ShowIf("resizeBgToFitTheScreen")][SerializeField] Image bgImage;

        [Header("Blur")]
        [SerializeField] bool hasBlur;
        [ShowIf("hasBlur")][SerializeField] Image blurImage;
        [ShowIf("hasBlur")][SerializeField] Image contentImage;

        bool isOpen;
        UnityAction CloseAction;
        RectTransform screenRect;

        #region Properties
        public bool IsOpen => isOpen;
        public bool IgnoreClosingOnAnotherPopUpOpen => ignoreClosingOnAnotherPopUpOpen;
        public bool CloseOtherPopUpsOnOpeningThis => closeOtherPopUpsOnOpeningThis;
        #endregion


        #region Initialization
        protected virtual void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => OpenClose(false));

            if (resizeBgToFitTheScreen && bgImage != null)
                FitBgToScreen();
        }

        public void Init(RectTransform screenRect)
        {
            this.screenRect = screenRect;
            content.SetActive(false);
        }
        #endregion


        #region Open / Close Logic
        /// <summary>
        /// Opens or closes this popup, optionally invoking a callback on close.
        /// </summary>
        public virtual void OpenClose(bool open, UnityAction CloseAction = null)
        {
            if (!open && !isOpen) return;
            isOpen = open;

            if (open)
            {
                this.CloseAction = CloseAction;
                UIManager.Instance.Popups?.OpeningPopUp(this);
            }
            else
            {
                UIManager.Instance.Popups?.ClosePopUp(this);
            }

            PlaySound(open);
            HandleAnimation(open);
            HandleBlur(open);
        }

        /// <summary>
        /// Invoked when animation completes to hide content and trigger callback.
        /// </summary>
        void Close()
        {
            CloseAction?.Invoke();
            content.SetActive(false);
        }
        #endregion


        #region Animation
        void HandleAnimation(bool open)
        {
            if (!haveAnimation)
            {
                if (!open) CloseAction?.Invoke();
                content.SetActive(open);
                return;
            }

            if (open)
            {
                if (smallPopUpAnim)
                {
                    // Small pop-up bounce animation
                    parentToAnimate.transform.localScale = Vector3.one * 0.7f;
                    LeanTween.scale(parentToAnimate, Vector3.one * 1.1f, 0.25f)
                        .setEase(LeanTweenType.easeOutBack)
                        .setOnComplete(() =>
                        {
                            LeanTween.scale(parentToAnimate, Vector3.one, 0.1f)
                                .setEase(LeanTweenType.easeOutQuad);
                        });

                    // Fade in background
                    Color col = Color.black; col.a = 0;
                    dimmedBackground.color = col;
                    LeanTween.value(dimmedBackground.gameObject, 0f, 0.8f, 0.25f)
                        .setEase(LeanTweenType.easeOutCubic)
                        .setOnUpdate(val =>
                        {
                            col.a = val;
                            dimmedBackground.color = col;
                        });
                }
                else
                {
                    anim.enabled = true;
                    anim.SetBool("Open", true);
                }

                content.SetActive(true);
            }
            else
            {
                if (smallPopUpAnim)
                {
                    // Scale down and fade background
                    LeanTween.scale(parentToAnimate, Vector3.one * 0.75f, 0.15f)
                        .setEase(LeanTweenType.easeInBack);

                    Color col = Color.black;
                    LeanTween.value(dimmedBackground.gameObject, 0.8f, 0f, 0.15f)
                        .setEase(LeanTweenType.easeInCubic)
                        .setOnUpdate(val =>
                        {
                            col.a = val;
                            dimmedBackground.color = col;
                        })
                        .setOnComplete(Close);
                }
                else
                {
                    anim.SetBool("Open", false);
                    LeanTween.delayedCall(closingAnimLenght, () =>
                    {
                        anim.enabled = false;
                        Close();
                    });
                }
            }
        }
        #endregion


        #region Sound & Blur
        void PlaySound(bool open)
        {
            if (uniqueSounds && (open ? openingSound.audioClip : closingSound.audioClip) != null)
            {
                SoundManager.Instance.PlaySound(open ? openingSound : closingSound);
            }
            else
            {
                SoundManager.Instance.PopUp(open, smallPopUp);
            }
        }

        void HandleBlur(bool open)
        {
            if (!hasBlur) return;

            if (open)
            {
                if (PlayerPrefs.GetInt("Quality") > 1)
                {
                    blurImage.enabled = true;
                    contentImage.enabled = false;
                    UIManager.Instance.BlurEffect.AnimateBlurIn();
                }
                else
                {
                    blurImage.enabled = false;
                    contentImage.enabled = true;
                }
            }
            else if (PlayerPrefs.GetInt("Quality") > 1)
            {
                UIManager.Instance.BlurEffect.AnimateBlurOut();
            }
        }
        #endregion


        #region Utility
        /// <summary>
        /// Scales background to match the current screen size while preserving sprite ratio.
        /// </summary>
        void FitBgToScreen()
        {
            float spriteWidth = bgImage.sprite.texture.width;
            float spriteHeight = bgImage.sprite.texture.height;
            float widthRatio = spriteWidth / screenRect.rect.width;
            float heightRatio = spriteHeight / screenRect.rect.height;

            bool widthCloser = Mathf.Abs(1 - widthRatio) < Mathf.Abs(1 - heightRatio);
            float multiplier = 1f / (widthCloser ? widthRatio : heightRatio);

            RectTransform imageRect = bgImage.GetComponent<RectTransform>();
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                widthCloser ? screenRect.rect.width : spriteWidth * multiplier);
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                widthCloser ? spriteHeight * multiplier : screenRect.rect.height);
        }
        #endregion
    }
}
