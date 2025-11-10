using Sirenix.OdinInspector;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Defines the visual and behavioral parameters for a HUD feedback message.
    /// 
    /// Each definition controls animation timing, scaling, tilt, movement, and optional sound.
    /// These are used by <see cref="GameHudFeedback"/> instances to drive their effects.
    /// </summary>
    [CreateAssetMenu(menuName = "Igronaut/ClumsyCrew/Defs/HudFeedbackDef")]
    public class HudFeedbackDef : ScriptableObject
    {
        [Header("General")]
        public GameHudFeedbackType type;
        public GameHudFeedback prefab;

        [Header("Options")]
        public bool hasAudioClip;
        public bool goUp;
        public bool goDownOnRelease;
        public bool tilt;

        [Header("Animation Timing")]
        [Tooltip("Rotation angle range for wobble effect.")]
        public float rotate = 5f;

        [Tooltip("Time to scale back to 1x size after first pop.")]
        public float backTime = 0.05f;

        [Tooltip("Time to scale up initially.")]
        public float upTime = 0.5f;

        [Tooltip("Time to scale down when disappearing.")]
        public float downTime = 0.4f;

        [Tooltip("How long the feedback stays visible before disappearing.")]
        public float stayTime = 1f;

        [Tooltip("Starting scale size of the elements.")]
        public float startSize = 0f;

        [Tooltip("Maximum target scale size.")]
        public float maxSize = 1.1f;

        [Tooltip("Delay between each sub-object animation.")]
        public float delayBetweenGOobjects = 0.03f;

        [Tooltip("Randomness range applied to end scale.")]
        public Vector2 gameEndSizeRandomness = new(1.02f, 1.1f);

        public LeanTweenType upLeanTween = LeanTweenType.easeInQuad;
        public LeanTweenType downLeanTween = LeanTweenType.easeInOutQuad;

        [ShowIf(nameof(hasAudioClip))]
        [Tooltip("Delay before playing the associated sound effect.")]
        public float playSoundAfter = 1f;

        [Header("Go Up Motion")]
        [ShowIf(nameof(goUp))] public float goUpStart = 0.2f;
        [ShowIf(nameof(goUp))] public float goUpTime = 0.5f;
        [ShowIf(nameof(goUp))] public float goUpLength = 50f;
        [ShowIf(nameof(goUp))] public LeanTweenType goUpTween = LeanTweenType.easeInOutQuad;

        [Header("Tilt Motion")]
        [ShowIf(nameof(tilt))] public float tiltStart = 0f;
        [ShowIf(nameof(tilt))] public int tiltTimes = 2;
        [ShowIf(nameof(tilt))] public float tiltRotation = 10f;
        [ShowIf(nameof(tilt))] public float tiltTime = 0.5f;

        [Header("Start Delay")]
        [Tooltip("Delay before this feedback animation starts.")]
        public float delayStartTime = 0f;

        [Header("Audio")]
        [ShowIf(nameof(hasAudioClip))] public AudioClip audioClip;
    }
}
