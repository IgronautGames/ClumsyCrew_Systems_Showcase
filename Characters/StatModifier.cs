using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Defines a single modifier applied to a stat or multiplier.
    /// Can be created by any source that implements <see cref="IModifierSource"/>.
    /// </summary>
    [System.Serializable]
    public struct StatModifier
    {
        public ModifierType modifierType;
        public float multiplier;
        public IModifierSource source;

        public readonly ModifierType ModifierType => modifierType;
        public readonly float Percent => (multiplier - 1f) * 100f;
        public readonly float Multiplier => multiplier;

        public StatModifier(ModifierType modifierType, float percentValue, IModifierSource source)
        {
            this.modifierType = modifierType;
            this.multiplier = 1f + (percentValue / 100f);
            this.source = source;
        }

        public void Apply(ref float value) => value *= multiplier;
        public void Remove(ref float value) => value /= multiplier;
    }
}
