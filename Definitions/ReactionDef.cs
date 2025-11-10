using Sirenix.OdinInspector;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// ScriptableObject defining a single character reaction.
    /// Used for all types of reactions like falling, hit, knockback, etc.
    ///
    /// Each reaction can define:
    /// - Animation trigger
    /// - Particles, sound, and voice effects
    /// - HUD feedback
    /// - Camera impulse
    /// - Chained next reaction
    ///
    /// The system allows designers to fine-tune responses without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Igronaut/ClumsyCrew/Defs/ReactionDef")]
    public class ReactionDef : ScriptableObject
    {
        [SerializeField] ReactionType type;

        [Header("Modules")]
        [SerializeField] bool hasAnimation;
        [SerializeField] bool hasParticles;
        [SerializeField] bool hasCameraImpulse;
        [SerializeField] bool hasHiglight;
        [SerializeField] bool hasHudFeedback;
        [SerializeField] bool hasSoundEffect;
        [SerializeField] bool hasVO;
        [SerializeField] bool chainedReaction;
        [SerializeField] bool recoverManually;

        [Header("Tweaking")]
        [SerializeField, HideIf("recoverManually")] float recoveryTime;
        [SerializeField, ShowIf("chainedReaction")] ReactionType nextReactionInChain;
        [SerializeField, ShowIf("hasParticles")] ReactionParticlesValues particlesValues;
        [SerializeField, ShowIf("hasAnimation")] AnimValueDef triggerAnimation;
        [SerializeField, ShowIf("hasCameraImpulse")] CameraShakeType cameraImpulse;
        [SerializeField, ShowIf("hasHiglight")] CharacterHiglightType highlight;
        [SerializeField, ShowIf("hasHudFeedback")] GameHudFeedbackType hudFeedback;
        [SerializeField, ShowIf("hasSoundEffect")] AudioClip soundEffect;
        [SerializeField, ShowIf("hasSoundEffect")] float soundEffectDelay;
        [SerializeField, ShowIf("hasVO")] VOType voType;

        #region Public Properties

        public ReactionType Type => type;
        public bool HasAnimation => hasAnimation;
        public bool HasParticles => hasParticles;
        public bool HasCameraImpulse => hasCameraImpulse;
        public bool HasHiglight => hasHiglight;
        public bool HasHudFeedback => hasHudFeedback;
        public bool HasSoundEffect => hasSoundEffect;
        public bool HasVO => hasVO;
        public bool ChainedReaction => chainedReaction;
        public bool RecoverManually => recoverManually;

        public float RecoveryTime => recoveryTime;
        public ReactionType NextReactionInChain => nextReactionInChain;
        public ReactionParticlesValues ParticlesValues => particlesValues;
        public AnimValueDef TriggerAnimation => triggerAnimation;
        public CameraShakeType CameraImpulse => cameraImpulse;
        public CharacterHiglightType Highlight => highlight;
        public GameHudFeedbackType HudFeedback => hudFeedback;
        public AudioClip SoundEffect => soundEffect;
        public float SoundEffectDelay => soundEffectDelay;
        public VOType VOType => voType;

        #endregion
    }
}
