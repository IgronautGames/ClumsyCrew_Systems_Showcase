using ClumsyCrew.Core;
using UnityEngine;
using UnityEngine.Events;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Represents a single animated HUD feedback element (e.g., “Perfect!”, “Double Smash!”, “Lost Balance!”).
    /// Handles all LeanTween-based scaling, rotation, tilt, and sound playback.
    /// </summary>
    public class GameHudFeedback : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameHudFeedbackType type;
        [SerializeField] RectTransform[] frontTrans;
        [SerializeField] RectTransform[] outlineTrans;
        [SerializeField] RectTransform parentRect;

        public GameHudFeedbackType Type => type;

        HudFeedbackDef def;
        UnityAction<GameHudFeedback> returnAction;
        Vector2 startPos;
        int tiltTimesLeft;
        bool isInitialized;

        public void Init(HudFeedbackDef def, UnityAction<GameHudFeedback> returnAction)
        {
            this.def = def;
            this.returnAction = returnAction;
        }

        void Start()
        {
            PlayAnimation();
        }

        void OnEnable()
        {
            if (isInitialized)
            {
                ResetTransforms();
                PlayAnimation();
            }
            else
            {
                isInitialized = true;
                startPos = parentRect.anchoredPosition;
            }
        }

        void ResetTransforms()
        {
            parentRect.anchoredPosition = startPos;

            foreach (var rect in frontTrans)
            {
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.identity;
            }

            foreach (var rect in outlineTrans)
            {
                rect.localScale = Vector3.zero;
                rect.localRotation = Quaternion.identity;
            }
        }

        void PlayAnimation()
        {
            if (def.hasAudioClip)
                LeanTween.delayedCall(def.playSoundAfter, () => SoundManager.Instance.PlaySound(def.audioClip));

            float delay = def.delayStartTime;

            for (int i = 0; i < frontTrans.Length; i++)
            {
                float rotation = def.rotate * Random.Range(0.5f, 1.5f);
                float scale = def.maxSize * Random.Range(def.gameEndSizeRandomnes.x, def.gameEndSizeRandomnes.y);

                AnimateElement(frontTrans[i], delay, scale, rotation, i == frontTrans.Length - 1);
                AnimateElement(outlineTrans[i], delay, scale, rotation, false);

                delay += def.delayBetweenGOobjects;
            }

            if (def.goUp)
            {
                LeanTween.delayedCall(def.goUpStart, () =>
                {
                    LeanTween.moveY(parentRect, parentRect.anchoredPosition.y + def.goUpLenght, def.goUPTime)
                        .setEase(def.goUpTween);
                });
            }

            if (def.tilt)
            {
                LeanTween.delayedCall(def.tiltStart, () =>
                {
                    tiltTimesLeft = def.tiltTimes;
                    Tilt();
                });
            }
        }

        void Tilt()
        {
            tiltTimesLeft--;

            LeanTween.rotate(parentRect, def.tiltRotation, def.tiltTime * 0.25f)
                .setEaseOutQuad()
                .setOnComplete(() =>
                {
                    LeanTween.rotate(parentRect, -def.tiltRotation, def.tiltTime * 0.5f)
                        .setEaseInOutQuad()
                        .setOnComplete(() =>
                        {
                            LeanTween.rotate(parentRect, 0, def.tiltTime * 0.25f)
                                .setEaseInQuad()
                                .setOnComplete(() =>
                                {
                                    if (tiltTimesLeft > 0)
                                        Tilt();
                                });
                        });
                });
        }

        void AnimateElement(RectTransform rect, float delay, float targetScale, float rotation, bool isLast)
        {
            rect.localScale = Vector3.one * def.startSize;

            LeanTween.delayedCall(delay, () =>
            {
                LeanTween.scale(rect, Vector3.one * targetScale, def.upTime)
                    .setEase(def.upLeanTween)
                    .setOnComplete(() =>
                    {
                        LeanTween.scale(rect, Vector3.one, def.backTime)
                            .setEase(def.downLeanTween)
                            .setOnComplete(() =>
                            {
                                if (!def.goDownOnRelease)
                                {
                                    LeanTween.delayedCall(def.stayTime, () =>
                                    {
                                        LeanTween.scale(rect, Vector3.one * targetScale, def.backTime)
                                            .setEase(def.upLeanTween)
                                            .setOnComplete(() =>
                                            {
                                                LeanTween.scale(rect, Vector3.zero, def.downTime)
                                                    .setEase(def.downLeanTween)
                                                    .setOnComplete(() =>
                                                    {
                                                        if (isLast)
                                                            returnAction?.Invoke(this);
                                                    });
                                            });
                                    });
                                }
                            });
                    });

                float total = def.upTime + def.backTime;

                LeanTween.rotate(rect, -rotation, total * 0.35f)
                    .setEaseOutQuad()
                    .setOnComplete(() =>
                    {
                        LeanTween.rotate(rect, rotation, total * 0.45f)
                            .setEaseInOutQuad()
                            .setOnComplete(() =>
                            {
                                LeanTween.rotate(rect, 0, total * 0.2f)
                                    .setEaseInOutQuad();
                            });
                    });
            });
        }

        public void Release()
        {
            for (int i = 0; i < frontTrans.Length; i++)
            {
                LeanTween.scale(frontTrans[i], Vector2.zero, def.downTime).setEase(def.downLeanTween);
                LeanTween.scale(outlineTrans[i], Vector2.zero, def.downTime).setEase(def.downLeanTween);
            }

            LeanTween.delayedCall(def.downTime, () => returnAction?.Invoke(this));
        }
    }
}
