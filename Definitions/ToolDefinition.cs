using System;
using System.Collections.Generic;
using System.Linq;
using ClumsyCrew.Characters;
using ClumsyCrew.Minigames;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Defines all parameters and behaviors for a player tool (e.g. hammer, pickaxe, mop).
    /// Controls animation sets, hit timing, charge mechanics, modifiers, and audiovisual feedback.
    /// </summary>
    [CreateAssetMenu(menuName = "Igronaut/ClumsyCrew/Defs/ToolDefinition")]
    public class ToolDefinition : ItemDefinition, ILevelBasedModifier
    {
        #region General
        [Header("General Settings")]
        [SerializeField] private bool spawnsPrefab;
        [ShowIf(nameof(spawnsPrefab))][SerializeField] private ToolScript toolPrefab;
        [ShowIf(nameof(spawnsPrefab))][SerializeField] private AnchorType toolPosition;

        [SerializeField] private bool hasUsingSound;
        [SerializeField] private bool dealDamageWithDetector;
        [ShowIf(nameof(dealDamageWithDetector))][SerializeField] private bool activateDetectorOnStart;

        [SerializeField] private bool hasToolButton;
        [SerializeField] private bool hasIdleAnim;
        [SerializeField] private bool hasActionButton;
        [SerializeField] private bool unloadOnItemDestroy;

        [SerializeField] private bool hasPreventionUntilHit;
        [ShowIf(nameof(hasActionButton))][SerializeField] private bool performActionOnButtonDown;
        [ShowIf(nameof(hasActionButton))][SerializeField] private bool progressSlider;
        [ShowIf(nameof(hasActionButton))][SerializeField] private bool powerBar;
        [ShowIf(nameof(powerBar))][SerializeField] private bool powerBarActivatedAllTime;

        [SerializeField] private bool lookAtTarget;
        [SerializeField] private bool useStat;
        [SerializeField] private bool hasModifiers;
        [SerializeField] private bool combo;
        [SerializeField] private bool charge;
        [SerializeField] private bool multihit;
        [SerializeField] private bool multiDestroy;
        #endregion

        #region Stat Influence
        [Header("Stat Influence")]
        [ShowIf(nameof(useStat))][SerializeField] private CharStatType useStatType;
        [ShowIf(nameof(hasModifiers))][SerializeField] private List<LevelModifier> levelModifiers;
        #endregion

        #region Animations
        [Header("Animations")]
        [ShowIf(nameof(hasIdleAnim))][SerializeField] private AnimValueDef idleAnim;
        [ShowIf(nameof(hasActionButton))][SerializeField] private AnimValueDef actionAnim;
        [HideIf(nameof(performActionOnButtonDown))][SerializeField] private AnimValueDef releaseAnim;

        [ShowIf(nameof(multihit))][SerializeField] private AnimValueDef multihitAnim;
        [ShowIf(nameof(combo))][SerializeField] private AnimValueDef comboAnim;
        [ShowIf(nameof(charge))][SerializeField] private AnimValueDef chargeAnim;
        #endregion

        #region Hit Timings & Camera Feedback
        [Header("Hit Timing & Camera Feedback")]
        [SerializeField] private float normalHitTime = 0.3f;
        [SerializeField] private CameraShakeType normalHitCameraShake;
        #endregion

        #region Multihit
        [Header("Multihit Settings")]
        [ShowIf(nameof(multihit))][SerializeField] private float multihitTimeWindow;
        [ShowIf(nameof(multihit))][SerializeField] private float multihitHitTime;
        [ShowIf(nameof(multihit))][SerializeField] private AudioClip multihitAudioClip;
        [ShowIf(nameof(multihit))][SerializeField] private CameraShakeType multihitCameraShake;
        #endregion

        #region Combo
        [Header("Combo Settings")]
        [ShowIf(nameof(combo))][SerializeField] private float comboWindow;
        [ShowIf(nameof(combo))][SerializeField] private int comboHitsRequired;
        [ShowIf(nameof(combo))][SerializeField] private float comboHitTime;
        [ShowIf(nameof(combo))][SerializeField] private CameraShakeType comboCameraShake;
        #endregion

        #region Charge
        [Header("Charge Settings")]
        [ShowIf(nameof(charge))][SerializeField] private float chargeTimeMax;
        [ShowIf(nameof(charge))][SerializeField] private float chargeTimeStartsAt;
        [ShowIf(nameof(charge))][SerializeField] private float chargeZoneStartsAt;
        [ShowIf(nameof(charge))][SerializeField] private List<ChargePercent> chargePercents;

        [ShowIf(nameof(charge))][SerializeField] private bool useChargeParticlesIndicator;
        [ShowIf(nameof(charge))][SerializeField] private bool useChargeAbilityInfluence;
        [ShowIf(nameof(useChargeAbilityInfluence))][SerializeField] private CharStatType chargeAbilityInfluenceType;
        #endregion

        #region Multi Destroy
        [Header("Multi-Destroy Settings")]
        [ShowIf(nameof(multiDestroy))][SerializeField] private float multiDestroyWindow;
        #endregion

        #region Audio
        [Header("Audio References")]
        [ShowIf(nameof(hasUsingSound))][SerializeField] private AudioClip useClip;
        [ShowIf(nameof(hasUsingSound))][SerializeField] private float clipDelay;
        #endregion

        #region Properties
        public bool HasUseSound => hasUsingSound;
        public AudioClip UseClip => useClip;
        public float ClipDelay => clipDelay;

        public bool MultiDestroy => multiDestroy;
        public float MultiDestroyWindow => multiDestroyWindow;
        public bool HasPreventionUntilHit => hasPreventionUntilHit;

        public CameraShakeType NormalHitCameraShake => normalHitCameraShake;
        public CameraShakeType MultihitCameraShake => multihitCameraShake;
        public CameraShakeType ComboCameraShake => comboCameraShake;

        public bool UnloadOnItemDestroy => unloadOnItemDestroy;

        public AnimValueDef ChargeAnim => chargeAnim;
        public float MultihitHitTime => multihitHitTime;
        public float NormalHitTime => normalHitTime;
        public AnimValueDef MultihitAnim => multihitAnim;
        public bool PerformActionOnButtonDown => performActionOnButtonDown;

        public bool UseStat => useStat;
        public CharStatType UseStatType => useStatType;
        public bool HasModifiers => levelModifiers != null && levelModifiers.Count > 0;

        public IReadOnlyList<StatModifier> GetModifiers() => Array.Empty<StatModifier>();
        public List<StatModifierData> GetModifiersForLevel(int level)
            => levelModifiers.FirstOrDefault(lm => lm.level == level)?.modifiers;

        public bool DealDamageWithDetector => dealDamageWithDetector;
        public bool ActivateDetectorOnStart => activateDetectorOnStart;
        public AnimValueDef ReleasingAnim => releaseAnim;

        public bool Multihit => multihit;
        public float MultihitTimeWindow => multihitTimeWindow;
        public AudioClip MultihitAudioClip => multihitAudioClip;

        public bool Combo => combo;
        public float ComboWindow => comboWindow;
        public int ComboHitsRequired => comboHitsRequired;
        public float ComboHitTime => comboHitTime;
        public AnimValueDef ComboAnim => comboAnim;

        public bool Charge => charge;
        public float ChargeTimeMax => chargeTimeMax;
        public float ChargeTimeStartsAt => chargeTimeStartsAt;
        public float ChargeZoneStartsAt => chargeZoneStartsAt;
        public List<ChargePercent> ChargePercents => chargePercents;

        public bool UseChargeAbilityInfluence => useChargeAbilityInfluence;
        public CharStatType ChargeAbilityInfluenceType => chargeAbilityInfluenceType;

        public AnchorType ToolPosition => toolPosition;
        public ToolScript ToolPrefab => toolPrefab;
        public bool SpawnsPrefab => spawnsPrefab;
        public bool HasToolButton => hasToolButton;
        public bool HasActionButton => hasActionButton;
        public bool PowerBar => powerBar;
        public bool PowerBarActivatedAllTime => powerBarActivatedAllTime;
        public bool ProgressSlider => progressSlider;
        public bool HasIdleAnim => hasIdleAnim;
        public AnimValueDef IdleAnim => idleAnim;
        public AnimValueDef ActionAnim => actionAnim;
        public bool UseChargeParticlesIndicator => useChargeParticlesIndicator;
        public bool LookAtTarget => lookAtTarget;
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure this ScriptableObject is always flagged as TOOL type.
            var so = new SerializedObject(this);
            so.FindProperty("type").enumValueIndex = (int)ItemType.TOOL;
            so.ApplyModifiedProperties();
        }
#endif
    }
}
