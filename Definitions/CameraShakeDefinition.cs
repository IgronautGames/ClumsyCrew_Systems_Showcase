using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Defines the visual intensity and feel of a specific camera shake type.
    /// These assets are referenced by gameplay systems (tools, reactions, weapons, etc.)
    /// so designers can easily tune feedback without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Igronaut/ClumsyCrew/Defs/CameraShakeDefinition")]
    public class CameraShakeDefinition : ScriptableObject
    {
        [Header("Shake Type")]
        [Tooltip("Unique identifier used by gameplay systems (e.g., 'HammerHit', 'Explosion').")]
        public CameraShakeType type;

        [Header("Shake Settings")]
        [Tooltip("Overall strength of the shake.")]
        public float amplitude = 1f;

        [Tooltip("Oscillation speed of the shake.")]
        public float frequency = 1f;

        [Tooltip("Duration of constant shake before decay begins.")]
        public float sustain = 0.1f;

        [Tooltip("How quickly the shake fades out after sustaining.")]
        public float decay = 0.3f;
    }
}
