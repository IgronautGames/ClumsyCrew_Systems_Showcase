using System.Collections.Generic;
using ClumsyCrew.Characters;
using ClumsyCrew.Minigames;
using ClumsyCrew.UI;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Central registry of all ScriptableObject definitions used in the game.
    /// 
    /// Loads, organizes, and exposes read-only dictionaries for quick lookup of:
    /// - Tools, Weapons, Machines, Customization, Traits, Items
    /// - Characters, Jobs, Areas, Reactions, Stats, Rarities, and more
    /// 
    /// Enables data-driven systems throughout the project.
    /// </summary>
    public class DefinitionsManager : MonoBehaviour
    {
        public static DefinitionsManager Instance;

        [Header("Configs")]
        [SerializeField] DefinitionsConfig config;
        [SerializeField] CharactersConfig charConfig;
        [SerializeField] RewardsConfig rewardsConfig;

        #region Internal Dictionaries
        Dictionary<JobItemType, JobItemDef> jobItemsDefsDict = new();
        Dictionary<ItemSubType, CurrencyDefinition> currencyDefsDict = new();
        Dictionary<ItemSubType, ToolDefinition> toolDefsDict = new();
        Dictionary<ItemSubType, ToolDefinition> allToolsDefsDict = new();
        Dictionary<ItemSubType, WeaponDefinition> weaponsDefsDict = new();
        Dictionary<ItemSubType, CustomizationItemDef> custDefsDict = new();
        Dictionary<ItemSubType, MachinesDef> machinesDefsDict = new();
        Dictionary<ItemSubType, ItemDefinition> itemsDefsDict = new();
        Dictionary<ItemSubType, TraitDefinition> traitsDefsDict = new();
        Dictionary<DailyMissionType, DailyMissionDef> dailyMissionDefsDict = new();
        Dictionary<string, JobDef> jobDefsDict = new();
        Dictionary<string, JobDef> locaMultiplayerJobDefsDict = new();
        Dictionary<MinigameType, List<JobDef>> minigamesPerType = new();
        Dictionary<ItemSubType, ChestDef> chestDefsDict = new();
        Dictionary<ItemType, ItemTypeDef> itemTypesDefDict = new();
        Dictionary<RarityType, RarityDef> rarityDefsDict = new();
        Dictionary<CharStatType, StatDef> abilityDefsDict = new();
        Dictionary<InfluenceType, InfluenceDef> influenceDefsDict = new();
        Dictionary<WeaponClass, WeaponClassDef> weaponClassDefsDict = new();
        Dictionary<RarityType, StorageDef> storageDefsDict = new();
        Dictionary<string, AreaDef> areasDefs = new();
        Dictionary<string, AreaDef> areasDefsPerJob = new();
        Dictionary<TemporaryItemType, TemporaryItemDef> tempItemsDefsDict = new();
        Dictionary<ModifierType, ModifierDef> modifiersDefsDict = new();
        Dictionary<RarityType, List<ItemSubType>> toolsByRarity = new();
        Dictionary<RarityType, List<ItemSubType>> weaponsByRarity = new();
        Dictionary<RarityType, List<ItemSubType>> machinesByRarity = new();
        Dictionary<RarityType, List<ItemSubType>> customizationByRarity = new();
        Dictionary<CustSubType, ItemSubType> defaultCustTypes = new();
        Dictionary<CharacterType, CharacterDef> charDefDict = new();
        Dictionary<CharacterType, CharacterDef> mainCharDefDict = new();
        Dictionary<CharacterType, CharacterDef> sideCharDefDict = new();
        Dictionary<ReactionType, ReactionDef> reactionsDefsDict = new();
        Dictionary<AnimationType, AnimValueDef> animsMainDict = new();
        Dictionary<AnimationType, AnimValueDef> animsNPCDict = new();
        Dictionary<CameraShakeType, CameraShakeDefinition> cameraShakesDict = new();
        Dictionary<EmojiType, EmojiDef> emojisDict = new();
        Dictionary<LanguageType, LanguageDef> languageDefDict = new();
        Dictionary<string, IAPItemDef> iapItemsDefsDict = new();
        Dictionary<ParticlesType, ParticleDef> particlesDefsDict = new();
        Dictionary<GameHudFeedbackType, HudFeedbackDef> hudFeedbackDefsDict = new();

        List<CharacterType> mainCharacterTypes = new();
        List<CharacterType> npcCharacterTypes = new();
        #endregion

        #region Public Accessors
        public IReadOnlyDictionary<GameHudFeedbackType, HudFeedbackDef> HudFeedbackDefsDict => hudFeedbackDefsDict;
        public IReadOnlyDictionary<ParticlesType, ParticleDef> ParticlesDefsDict => particlesDefsDict;
        public IReadOnlyDictionary<string, IAPItemDef> IapItemsDefsDict => iapItemsDefsDict;
        public IReadOnlyDictionary<LanguageType, LanguageDef> LanguageDefDict => languageDefDict;
        public IReadOnlyDictionary<EmojiType, EmojiDef> EmojisDict => emojisDict;
        public IReadOnlyDictionary<ItemSubType, CurrencyDefinition> CurrencyDefsDict => currencyDefsDict;
        public IReadOnlyDictionary<ItemSubType, ToolDefinition> ToolDefsDict => allToolsDefsDict;
        public IReadOnlyDictionary<ItemSubType, WeaponDefinition> WeaponsDefsDict => weaponsDefsDict;
        public IReadOnlyDictionary<ItemSubType, CustomizationItemDef> CustDefsDict => custDefsDict;
        public IReadOnlyDictionary<ItemSubType, MachinesDef> MachinesDefsDict => machinesDefsDict;
        public IReadOnlyDictionary<ItemSubType, ItemDefinition> ItemsDefsDict => itemsDefsDict;
        public IReadOnlyDictionary<JobItemType, JobItemDef> JobItemsDefsDict => jobItemsDefsDict;
        public IReadOnlyDictionary<DailyMissionType, DailyMissionDef> DailyMissionDefsDict => dailyMissionDefsDict;
        public IReadOnlyDictionary<string, JobDef> JobDefsDict => jobDefsDict;
        public IReadOnlyDictionary<ItemSubType, ChestDef> ChestDefsDict => chestDefsDict;
        public IReadOnlyDictionary<ItemType, ItemTypeDef> ItemTypesDefDict => itemTypesDefDict;
        public IReadOnlyDictionary<RarityType, RarityDef> RarityDefsDict => rarityDefsDict;
        public IReadOnlyDictionary<CharStatType, StatDef> StatsDefsDict => abilityDefsDict;
        public IReadOnlyDictionary<InfluenceType, InfluenceDef> InfluenceDefsDict => influenceDefsDict;
        public IReadOnlyDictionary<WeaponClass, WeaponClassDef> WeaponClassDefsDict => weaponClassDefsDict;
        public IReadOnlyDictionary<RarityType, StorageDef> StorageDefDict => storageDefsDict;
        public IReadOnlyDictionary<string, AreaDef> AreasDefs => areasDefs;
        public IReadOnlyDictionary<string, AreaDef> AreasDefsPerJob => areasDefsPerJob;
        public IReadOnlyDictionary<ItemSubType, TraitDefinition> TraitsDefsDict => traitsDefsDict;
        public IReadOnlyDictionary<TemporaryItemType, TemporaryItemDef> TempItemsDefsDict => tempItemsDefsDict;
        public IReadOnlyDictionary<ModifierType, ModifierDef> ModifiersDefsDict => modifiersDefsDict;
        public IReadOnlyDictionary<RarityType, List<ItemSubType>> ToolsByRarity => toolsByRarity;
        public IReadOnlyDictionary<RarityType, List<ItemSubType>> WeaponsByRarity => weaponsByRarity;
        public IReadOnlyDictionary<RarityType, List<ItemSubType>> MachinesByRarity => machinesByRarity;
        public IReadOnlyDictionary<RarityType, List<ItemSubType>> CustomizationByRarity => customizationByRarity;
        public IReadOnlyDictionary<CustSubType, ItemSubType> DefaultCustTypes => defaultCustTypes;
        public IReadOnlyDictionary<CharacterType, CharacterDef> CharDefDict => charDefDict;
        public IReadOnlyDictionary<CharacterType, CharacterDef> MainCharDefDict => mainCharDefDict;
        public IReadOnlyDictionary<CharacterType, CharacterDef> SideCharDefDict => sideCharDefDict;
        public IReadOnlyDictionary<ReactionType, ReactionDef> ReactionsDefsDict => reactionsDefsDict;
        public Dictionary<AnimationType, AnimValueDef> AnimsMainDict => animsMainDict;
        public Dictionary<AnimationType, AnimValueDef> AnimsNPCDict => animsNPCDict;
        public List<CharacterType> MainCharacterTypes => mainCharacterTypes;
        public List<CharacterType> NpcCharacterTypes => npcCharacterTypes;
        public IReadOnlyDictionary<MinigameType, List<JobDef>> MinigamesPerType => minigamesPerType;
        public IReadOnlyDictionary<CameraShakeType, CameraShakeDefinition> CameraShakesDict => cameraShakesDict;
        #endregion


        #region Initialization
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeDictionaries();
        }

        /// <summary>
        /// Builds all definition dictionaries from the assigned config assets.
        /// </summary>
        void InitializeDictionaries()
        {
            // --- Rarity categories ---
            foreach (var rarity in config.RarityDefs)
            {
                toolsByRarity.Add(rarity.type, new());
                weaponsByRarity.Add(rarity.type, new());
                machinesByRarity.Add(rarity.type, new());
                customizationByRarity.Add(rarity.type, new());
            }

            // --- Modifiers, Temp items, Traits ---
            foreach (var def in config.ModifierDefs)
                modifiersDefsDict.Add(def.Type, def);

            foreach (var def in config.TempItemsDefs)
                tempItemsDefsDict.Add(def.Type, def);

            foreach (var def in config.TraitDefinitions)
                traitsDefsDict.Add(def.SubType, def);

            // --- Areas ---
            foreach (var area in config.AreaDefs)
            {
                areasDefs.Add(area.Id, area);
                foreach (var job in area.JobsInThisArea)
                    areasDefsPerJob.Add(job.ID, area);
            }

            // --- Misc definitions ---
            foreach (var def in config.StorageDefs)
                storageDefsDict.Add(def.Rarity, def);

            foreach (var def in config.WeaponClassDefs)
                weaponClassDefsDict.Add(def.Type, def);

            foreach (var def in config.InfluenceDefs)
                influenceDefsDict.Add(def.type, def);

            foreach (var def in config.StatsDefs)
                abilityDefsDict.Add(def.Type, def);

            foreach (var def in config.RarityDefs)
                rarityDefsDict.Add(def.type, def);

            foreach (var def in config.ItemTypeDefs)
                itemTypesDefDict.Add(def.type, def);

            // --- Job Items & Jobs ---
            foreach (var def in config.JobItemsDefs)
                jobItemsDefsDict.Add(def.JobItemType, def);

            foreach (var def in config.JobDefs)
            {
                if (def.MinigameType == MinigameType.Solo)
                    jobDefsDict.Add(def.ID, def);
                else if (def.MinigameType == MinigameType.LocalMultiplayer)
                    locaMultiplayerJobDefsDict.Add(def.ID, def);

                if (!minigamesPerType.ContainsKey(def.MinigameType))
                    minigamesPerType.Add(def.MinigameType, new());

                minigamesPerType[def.MinigameType].Add(def);
            }

            // --- Items ---
            foreach (var def in config.ItemDefs)
            {
                switch (def)
                {
                    case ToolDefinition tool:
                        toolDefsDict.Add(tool.SubType, tool);
                        allToolsDefsDict.Add(tool.SubType, tool);
                        toolsByRarity[tool.RariryType].Add(tool.SubType);
                        break;

                    case CurrencyDefinition curr:
                        currencyDefsDict.Add(curr.SubType, curr);
                        break;

                    case MachinesDef machine:
                        machinesDefsDict.Add(machine.SubType, machine);
                        machinesByRarity[machine.RariryType].Add(machine.SubType);
                        break;

                    case WeaponDefinition weapon:
                        weaponsDefsDict.Add(weapon.SubType, weapon);
                        weaponsByRarity[weapon.RariryType].Add(weapon.SubType);
                        break;

                    case CustomizationItemDef cust:
                        custDefsDict.Add(cust.SubType, cust);
                        if (cust.IsDefaultOfThisCustType)
                            defaultCustTypes.Add(cust.CustSubType, cust.SubType);
                        customizationByRarity[cust.RariryType].Add(cust.SubType);
                        break;

                    case ChestDef chest:
                        chestDefsDict.Add(chest.SubType, chest);
                        break;
                }

                itemsDefsDict.Add(def.SubType, def);
            }

            foreach (var def in config.AdditionalToolsDefs)
                allToolsDefsDict.Add(def.SubType, def);

            // --- Daily Missions ---
            foreach (var def in rewardsConfig.DailyMissionDefs)
                dailyMissionDefsDict.Add(def.Type, def);

            // --- Characters ---
            foreach (var def in charConfig.MainCharactersDefs)
            {
                charDefDict.Add(def.Type, def);
                mainCharDefDict.Add(def.Type, def);
                mainCharacterTypes.Add(def.Type);
            }

            foreach (var def in charConfig.SideCharactersDefs)
            {
                charDefDict.Add(def.Type, def);
                sideCharDefDict.Add(def.Type, def);
                npcCharacterTypes.Add(def.Type);
            }

            charDefDict.Add(charConfig.TutorialGuideDef.Type, charConfig.TutorialGuideDef);

            // --- Reactions, animations, effects ---
            foreach (var def in config.ReactionDefs)
                reactionsDefsDict.Add(def.Type, def);

            foreach (var def in charConfig.MainCharAnimDefs)
                animsMainDict.Add(def.Type, def);

            foreach (var def in charConfig.SideCharAnimDefs)
                animsNPCDict.Add(def.Type, def);

            foreach (var def in config.CameraShakesDefs)
                cameraShakesDict.Add(def.type, def);

            foreach (var def in config.EmojiDefs)
                emojisDict.Add(def.type, def);

            foreach (var def in config.LanguafeDefs)
                languageDefDict.Add(def.LanguageType, def);

            foreach (var def in config.IAPDefs)
                iapItemsDefsDict.Add(def.ID, def);

            foreach (var def in config.ParticleDefs)
                particlesDefsDict.Add(def.Type, def);

            foreach (var def in config.HudFeedbackDefs)
                hudFeedbackDefsDict.Add(def.type, def);
        }
        #endregion


        #region Helpers
        /// <summary>
        /// Returns a random item subtype of given rarity and type.
        /// </summary>
        public ItemSubType GetRandomSubTypeByRarity(ItemType type, RarityType rarType)
        {
            ItemSubType subType = ItemSubType.NONE;

            if (type == ItemType.WEAPON)
                subType = weaponsByRarity[rarType][Random.Range(0, weaponsByRarity[rarType].Count)];
            else if (type == ItemType.TOOL)
                subType = toolsByRarity[rarType][Random.Range(0, toolsByRarity[rarType].Count)];
            else if (type == ItemType.MACHINE)
                subType = machinesByRarity[rarType][Random.Range(0, machinesByRarity[rarType].Count)];
            else if (type == ItemType.CUSTOMIZATION)
                subType = customizationByRarity[rarType][Random.Range(0, customizationByRarity[rarType].Count)];
            else if (type == ItemType.NONE)
            {
                ItemType randomType;
                float ranValue = Random.value;

                if (ranValue < 0.33f)
                    randomType = ItemType.TOOL;
                else if (ranValue < 0.66f)
                    randomType = ItemType.WEAPON;
                else
                    randomType = ItemType.CUSTOMIZATION;

                subType = GetRandomSubTypeByRarity(randomType, rarType);
            }

            return subType;
        }
        #endregion
    }
}
