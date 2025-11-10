using ClumsyCrew.Core;
using ClumsyCrew.Minigames;
using ClumsyCrew.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Central system that manages all player and NPC characters:
    /// - Handles loading, saving, and unlocking of characters.
    /// - Spawns characters and NPCs for minigames or test scenes.
    /// - Tracks customization, traits, and levels per character.
    /// - Listens to UI popups and game state events to attach/detach callbacks.
    /// </summary>
    public class CharactersManager : MonoBehaviour
    {
        public static CharactersManager Instance;

        public event UnityAction MainCharChanged;
        public event UnityAction OnCharacterUnlock;
        public event UnityAction<CharStatType, ItemSubType> CharacterUpgraded;
        public event UnityAction<MainCharacterScript> OnMainCharacterSpawned;
        public event UnityAction<NPCScript> OnNPCSpawned;

        [SerializeField] CharactersConfig characterConfig;
        [SerializeField] CharacterSelectionController selection;
        [SerializeField] CustomizationController customization;
        [SerializeField] VOController vos;
        [SerializeField] CharacterInitializationFactory characterFactory;

        CharactersSaveData saveData;
        IReadOnlyDictionary<CharacterType, CharacterDef> charDefDict;

        readonly List<NPCScript> npcCharacters = new();
        readonly List<MainCharacterScript> mainCharacters = new();
        readonly List<CharacterScript> allCharacters = new();

        CharacterType lastSelectedChar;

        #region Properties
        public VOController Vos => vos;
        public CustomizationController Customization => customization;
        public CharacterSelectionController Selection => selection;
        public CharactersSaveData SaveData => saveData;
        public CharValues CurrentCharStats => saveData.charValues[lastSelectedChar];
        public IReadOnlyDictionary<CustSubType, ItemSubType> CustItemsEquipedOnCurrentChar => saveData.charValues[lastSelectedChar].custItemsEquiped;
        public List<NPCScript> NPCCharacters => npcCharacters;
        public List<MainCharacterScript> MainCharacters => mainCharacters;
        public List<CharacterScript> AllCharacters => allCharacters;
        #endregion


        #region Initialization
        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        void Start()
        {
            charDefDict = DefinitionsManager.Instance.CharDefDict;

            GameManager.Instance.GameStateChanged += GameStateChanged;
            UIManager.Instance.Popups.OnPopUpOpened += OnPopUpOpened;

            if (GameConfig.Instance.GameType != GameType.TestingLevel)
            {
                MinigameManager.Instance.OnMinigameSet += InstantiateCharacters;
                selection.OnCharacterChanged += SelectCharacter;
            }
            else
            {
                FeatureTestSceneManager.Instance.OnAddPlayer += SpawnMainCharacter;
                FeatureTestSceneManager.Instance.OnAddNPC += SpawnNPC;
            }
        }

        /// <summary>
        /// Initializes character save data either from SaveManager or default definitions.
        /// </summary>
        void LoadData()
        {
            saveData = new()
            {
                charValues = new(),
                charactersUnlocked = new(),
                characterSelected = CharacterType.Aljo
            };

            IReadOnlyList<CharacterDef> mainCharactersDefs = characterConfig.MainCharactersDefs;

            // Base values for each character
            for (int i = 0; i < mainCharactersDefs.Count; i++)
            {
                saveData.charValues.Add(mainCharactersDefs[i].Type, new CharValues
                {
                    levels = new(),
                    custItemsEquiped = new(),
                    activeTraits = new()
                });

                // Default traits
                for (int j = 0; j < mainCharactersDefs[i].Traits.Count; j++)
                    saveData.charValues[mainCharactersDefs[i].Type].activeTraits.Add(mainCharactersDefs[i].Traits[j].type);

                // Default customization slots
                for (int j = 0; j < (int)CustSubType.FACEMASK; j++)
                    saveData.charValues[mainCharactersDefs[i].Type].custItemsEquiped.Add((CustSubType)j, ItemSubType.HatsDefault);
            }

            CharactersSaveData loadedData = SaveManager.Instance?.State.charactersSaveData;

            // Load saved data
            if (loadedData != null)
            {
                saveData.characterSelected = loadedData.characterSelected;
                lastSelectedChar = saveData.characterSelected;

                for (int i = 0; i < loadedData.charactersUnlocked.Count; i++)
                    saveData.charactersUnlocked.Add(loadedData.charactersUnlocked[i]);

                foreach (var item in loadedData.charValues)
                {
                    for (int i = 0; i < item.Value.levels.Count; i++)
                        saveData.charValues[item.Key].levels.Add(item.Value.levels[i]);

                    foreach (var cusEq in item.Value.custItemsEquiped)
                        saveData.charValues[item.Key].custItemsEquiped[cusEq.Key] = cusEq.Value;
                }
            }
            // Initialize default state for new players
            else
            {
                saveData.characterSelected = CharacterType.Aljo;
                lastSelectedChar = CharacterType.Aljo;

                IReadOnlyList<CustomizationItemDef> customDefaultItemsDefs = characterConfig.CustomDefaultItemsDefs;
                for (int i = 0; i < mainCharactersDefs.Count; i++)
                {
                    for (int j = 0; j < mainCharactersDefs[i].StartLevels.Length; j++)
                        saveData.charValues[mainCharactersDefs[i].Type].levels.Add(mainCharactersDefs[i].StartLevels[j]);

                    for (int j = 0; j < customDefaultItemsDefs.Count; j++)
                        saveData.charValues[mainCharactersDefs[i].Type].custItemsEquiped[customDefaultItemsDefs[j].CustSubType] = customDefaultItemsDefs[j].SubType;
                }

                if (GameConfig.Instance.GameType == GameType.TestingLevel || GameConfig.Instance.GameType == GameType.NoTutorials)
                    saveData.charactersUnlocked.Add(lastSelectedChar);
            }
        }
        #endregion


        #region Event Listeners
        void OnPopUpOpened(PopUp popup, bool opened)
        {
            if (popup is ChoseCharacterPopUp choseChar)
            {
                if (opened)
                {
                    choseChar.OnTryUpgradeStat += TryUpgradeStat;
                    choseChar.UnlockCharPopUp.OnCharUnlock += TryUnlockChar;
                }
                else
                {
                    choseChar.OnTryUpgradeStat -= TryUpgradeStat;
                    choseChar.UnlockCharPopUp.OnCharUnlock -= TryUnlockChar;
                }
            }
            else if (popup is CustomizationPopUp cust)
            {
                if (opened)
                    cust.OnItemEquiped += EquipCustItem;
                else
                    cust.OnItemEquiped -= EquipCustItem;
            }
        }

        void GameStateChanged(GameState state)
        {
            if (state == GameState.Menu)
                DestroyCharacters();
        }
        #endregion


        #region Character Spawning
        /// <summary>
        /// Creates characters and NPCs for the currently active minigame.
        /// </summary>
        void InstantiateCharacters(MinigameScript minigame)
        {
            foreach (var slot in GameManager.Instance.Players.GetConfirmedSlots())
            {
                slot.navigator.ClearEvents();
                slot.Dispose();
            }

            JobDef def = GameManager.Instance.CurrentJobDef;

            if (def.MinigameType == MinigameType.Solo || def.MinigameType == MinigameType.Tutorials)
            {
                MainCharacterScript mainChar = Instantiate(PrefabsConfig.Instance.MainCharPrefab, transform);
                mainCharacters.Add(mainChar);
                allCharacters.Add(mainChar);
                mainChar.Init(characterFactory.CreateRealInitCharData(def, saveData));

                if (def.MinigameType == MinigameType.Solo)
                {
                    NPCScript client = Instantiate(def.HostChar.Prefab, transform);
                    npcCharacters.Add(client);
                    allCharacters.Add(client);
                    client.Init(characterFactory.CreateNPCInitData(def.HostChar, 1));
                }
            }
            else if (def.MinigameType == MinigameType.LocalMultiplayer)
            {
                List<PlayerSlot> slots = GameManager.Instance.Players.GetConfirmedSlots();
                for (int i = 0; i < slots.Count; i++)
                {
                    SpawnMainCharacter(minigame.SpawnPoints.Find(x => x.PlayerNumb == slots[i].index).SpawnTrans, i == slots.Count - 1);
                }

                foreach (var spawnPoint in minigame.SpawnPoints)
                {
                    if (spawnPoint.Role == CharacterRole.NPC)
                        SpawnNPC(spawnPoint);
                }
            }
        }

        /// <summary>
        /// Spawns a playable main character and assigns it to a player slot.
        /// </summary>
        void SpawnMainCharacter(Transform spawnTrans, bool updateRTs)
        {
            int mainCharsNumb = mainCharacters.Count;

            MainCharacterScript mainChar = Instantiate(PrefabsConfig.Instance.MainCharPrefab, transform);
            mainCharacters.Add(mainChar);
            allCharacters.Add(mainChar);

            PlayerSlot slot = GameManager.Instance.Players.GetSlot(mainCharsNumb);
            slot.character = mainChar;
            SceneManager.Instance.CameraController.AssignCameraToSlot(slot, updateRTs);

            CharacterInitializationData data = characterFactory.CreateLocalMultiCharData(DefinitionsConfig.Instance.TestingLevelDef, slot);
            mainChar.Init(data);
            mainChar.transform.SetPositionAndRotation(spawnTrans.position, spawnTrans.rotation);

            OnMainCharacterSpawned?.Invoke(mainChar);
        }

        /// <summary>
        /// Spawns an NPC character from a spawn point.
        /// </summary>
        void SpawnNPC(CharacterSpawnPoint spawnPoint)
        {
            CharacterDef charDef = DefinitionsManager.Instance.CharDefDict[spawnPoint.Type];
            NPCScript npc = Instantiate(charDef.Prefab, transform);
            npcCharacters.Add(npc);
            allCharacters.Add(npc);

            CharacterInitializationData data = characterFactory.CreateNPCInitData(charDef, allCharacters.Count - 1);
            data.ignoreWeapons = spawnPoint.IgnoreWeapons;

            npc.transform.SetPositionAndRotation(spawnPoint.SpawnTrans.position, spawnPoint.SpawnTrans.rotation);
            npc.Init(data);
            npc.AssignNPCRoler(spawnPoint.NPCRole);

            OnNPCSpawned?.Invoke(npc);
        }

        /// <summary>
        /// Destroys all spawned characters and resets lists.
        /// </summary>
        void DestroyCharacters()
        {
            foreach (var slot in GameManager.Instance.Players.GetConfirmedSlots())
                slot.navigator.ClearEvents();

            foreach (var ch in allCharacters)
            {
                if (ch is MainCharacterScript main)
                {
                    Destroy(main.Camera.gameObject);
                    Destroy(main.HudController.gameObject);
                }
                Destroy(ch.gameObject);
            }

            npcCharacters.Clear();
            mainCharacters.Clear();
            allCharacters.Clear();
        }
        #endregion


        #region Character Manipulation
        void TryUpgradeStat(CharStatType type, ItemSubType tokenType)
        {
            if (saveData.charValues[charDefDict[lastSelectedChar].Type].levels[(int)type] >= 100)
                return;

            if (InventoryManager.Instance.SaveData.itemsInventory[tokenType] >= 0)
            {
                saveData.charValues[charDefDict[lastSelectedChar].Type].levels[(int)type]++;
                CharacterUpgraded?.Invoke(type, tokenType);
            }
        }

        void TryUnlockChar(CharacterType type)
        {
            if (InventoryManager.Instance.TryBuy(charDefDict[type].Price))
            {
                saveData.charactersUnlocked.Add(type);
                OnCharacterUnlock?.Invoke();
            }
        }

        void SelectCharacter()
        {
            lastSelectedChar = selection.LastSelectedChar;
            saveData.characterSelected = lastSelectedChar;
            MainCharChanged?.Invoke();
        }

        void EquipCustItem(ItemSubType type)
        {
            if (InventoryManager.Instance.ItemsInventory[type] > 0)
            {
                var custSub = DefinitionsManager.Instance.CustDefsDict[type].CustSubType;
                saveData.charValues[lastSelectedChar].custItemsEquiped[custSub] = type;
            }
        }

        void ChosingFirstCharAfterFAll(int numb)
        {
            UIManager.Instance.Popups.GetPopUp<ChoseCharOnStartPopUp>().OnAfterFall -= ChosingFirstCharAfterFAll;

            saveData.charactersUnlocked.Add((CharacterType)numb);
            saveData.characterSelected = (CharacterType)numb;
        }
        #endregion
    }
}
