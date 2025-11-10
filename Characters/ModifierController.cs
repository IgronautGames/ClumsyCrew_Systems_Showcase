using System;
using System.Collections.Generic;
using System.Linq;
using ClumsyCrew.Characters;
using ClumsyCrew.UI;
using I2.Loc;
using UnityEngine;
using UnityEngine.Events;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Central system that manages all character modifiers.
    /// Handles stat calculations from:
    /// - Equipped items
    /// - Traits
    /// - Temporary boosts
    /// - Level-based modifiers
    ///
    /// Automatically updates stats and fires events when modifiers change.
    /// </summary>
    public class ModifierController : MonoBehaviour
    {
        public event UnityAction<ModifierType> OnModifierChanged;

        CharacterScript character;

        Dictionary<ModifierType, float> multipliers = new();
        Dictionary<CharStatType, int> baseStats = new();
        Dictionary<CharStatType, float> modifiedStats = new();

        readonly List<IModifierSource> flatSources = new();
        List<(ILevelBasedModifier source, int level)> levelSources = new();
        Dictionary<ModifierType, List<IModifierSource>> modifierContributors = new();

        IReadOnlyDictionary<ItemSubType, CustomizationItemDef> customDefsDict;
        IReadOnlyDictionary<ItemSubType, TraitDefinition> traitsDefsDict;

        public IReadOnlyDictionary<ModifierType, float> Modifiers => multipliers;
        public IReadOnlyDictionary<CharStatType, float> ModifiedStats => modifiedStats;

        public IModifierSource GetRandomContributor(ModifierType type)
        {
            if (!modifierContributors.TryGetValue(type, out var sources) || sources.Count == 0)
                return null;

            return sources[UnityEngine.Random.Range(0, sources.Count)];
        }

        void Awake()
        {
            foreach (CharStatType type in Enum.GetValues(typeof(CharStatType)))
            {
                baseStats.Add(type, 1);
                modifiedStats.Add(type, 1);
            }

            foreach (ModifierType type in Enum.GetValues(typeof(ModifierType)))
            {
                multipliers.Add(type, 1f);
                modifierContributors.Add(type, new List<IModifierSource>());
            }
        }

        void Start()
        {
            if (character is MainCharacterScript main)
            {
                main.ToolController.ItemWithLevelModifierEquiped += LevelItemEquipped;
                main.TemporaryItemsController.ItemWithLevelModifierEquiped += LevelItemEquipped;
                main.CarryingLogic.ItemWithModifierEquiped += ItemEquiped;
                main.ToolUseController.OnCombo += OnCombo;
                main.ToolUseController.OnMultihit += OnMultihit;
            }
        }

        #region Initialization

        public void Init(CharacterScript character)
        {
            this.character = character;
        }

        public void InitEquippement(List<int> levels, Dictionary<CustSubType, ItemSubType> custItemsEquiped, List<ItemSubType> traits)
        {
            customDefsDict = DefinitionsManager.Instance.CustDefsDict;
            traitsDefsDict = DefinitionsManager.Instance.TraitsDefsDict;

            for (int i = 0; i < levels.Count; i++)
                baseStats[(CharStatType)i] = levels[i];

            foreach (var cust in custItemsEquiped.Values)
            {
                if (customDefsDict.ContainsKey(cust))
                    foreach (var modifier in customDefsDict[cust].GetModifiers())
                        AddSource(modifier.source, false);
            }

            foreach (var trait in traits)
            {
                if (traitsDefsDict[trait].IsAppliedAutomatically)
                    AddSource(traitsDefsDict[trait], false);
            }

            RecalculateModifiers();

            for (int i = 0; i < levels.Count; i++)
                OnModifierChanged?.Invoke((ModifierType)i);
        }

        #endregion

        #region Event Handlers

        void ItemEquiped(IModifierSource source, bool equipped, bool recalculate)
        {
            if (equipped)
                AddSource(source, recalculate);
            else
                RemoveSource(source, recalculate);
        }

        void LevelItemEquipped(ILevelBasedModifier source, int level, bool equipped, bool recalculate)
        {
            if (equipped)
                AddSource(source, level, recalculate);
            else
                RemoveLeveldSource(source, level, recalculate);
        }

        void OnCombo()
        {
            CheckForModifiers(ModifierType.ComboToSpeedBoostChance);
        }

        void OnMultihit(int multiplier)
        {
            CheckForModifiers(ModifierType.ComboToSpeedBoostChance);
        }

        #endregion

        #region Core Modifier Logic

        public float GetMultiplier(ModifierType type)
        {
            return multipliers.TryGetValue(type, out var value) ? value : 1f;
        }

        void AddSource(IModifierSource source, bool recalculate = true)
        {
            if (flatSources.Contains(source)) return;

            flatSources.Add(source);

            if (recalculate)
            {
                RecalculateModifiers();
                foreach (var item in source.GetModifiers())
                    OnModifierChanged?.Invoke(item.ModifierType);
            }
        }

        void AddSource(ILevelBasedModifier source, int level, bool recalculate = true)
        {
            if (levelSources.Any(s => s.source == source)) return;

            levelSources.Add((source, level));

            if (recalculate)
            {
                RecalculateModifiers();
                foreach (var item in source.GetModifiersForLevel(level))
                    OnModifierChanged?.Invoke(item.modifierType);
            }
        }

        void RemoveSource(IModifierSource source, bool recalculate = true)
        {
            flatSources.Remove(source);

            if (recalculate)
            {
                RecalculateModifiers();
                foreach (var item in source.GetModifiers())
                    OnModifierChanged?.Invoke(item.ModifierType);
            }
        }

        void RemoveLeveldSource(ILevelBasedModifier source, int level, bool recalculate = true)
        {
            levelSources.RemoveAll(s => s.source == source);

            if (recalculate)
            {
                RecalculateModifiers();
                foreach (var item in source.GetModifiersForLevel(level))
                    OnModifierChanged?.Invoke(item.modifierType);
            }
        }

        void RecalculateModifiers()
        {
            foreach (var list in modifierContributors.Values)
                list.Clear();

            foreach (var key in multipliers.Keys.ToList())
                multipliers[key] = 1f;

            foreach (var source in flatSources)
                foreach (var mod in source.GetModifiers())
                    ApplyMod(mod);

            foreach (var (source, level) in levelSources)
                foreach (var modData in source.GetModifiersForLevel(level))
                    ApplyMod(modData.ToRuntime(source));

            RecalculateStatModifiers();
        }

        void RecalculateStatModifiers()
        {
            foreach (var kvp in baseStats)
            {
                float multiplier = GetMultiplier((ModifierType)kvp.Key);
                modifiedStats[kvp.Key] = kvp.Value * multiplier;
            }
        }

        void ApplyMod(StatModifier mod)
        {
            if (!multipliers.ContainsKey(mod.ModifierType))
                multipliers[mod.ModifierType] = 1f;

            multipliers[mod.ModifierType] *= mod.Multiplier;
            modifierContributors[mod.ModifierType].Add(mod.source);
        }

        #endregion

        #region Trait Activation

        public bool CheckForModifiers(ModifierType type, bool showMessage = true, int numb = 1)
        {
            bool result = false;

            if ((multipliers[type] - 1) > UnityEngine.Random.value)
            {
                IModifierSource contributor = GetRandomContributor(type);

                if (showMessage && contributor is TraitDefinition trait)
                    UIManager.Instance.Info.TraitTriggered(trait, GetTraitActivationMessage(contributor, numb));

                result = true;
            }

            return result;
        }

        string GetTraitActivationMessage(IModifierSource source, int numb = 1)
        {
            switch (source)
            {
                case TraitDefinition trait:
                    SoundManager.Instance.PlaySound(trait.AppearingSOund);
                    return LocalizationManager.GetTranslation(trait.SubType.ToString() + "Short" + numb);
            }
            return "";
        }

        #endregion

        #region Debug

        void DebugModifiedStats()
        {
            Debug.Log("==== Modified Stats ====");
            foreach (var kvp in modifiedStats)
                Debug.Log($"{kvp.Key}: {kvp.Value:F2}");
            Debug.Log("========================");
        }

        #endregion
    }
}
