using ClumsyCrew.Core;
using ClumsyCrew.Minigames;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Core base class for all characters (Main Player, NPCs, Tutorial characters, etc.).
    /// 
    /// Responsible for:
    /// - Holding and initializing all character-related subsystems (Animator, Movement, Detection, Modifiers, etc.)
    /// - Defining shared data like levels, traits, and equipment.
    /// - Setting up character role–specific logic (Player vs NPC).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(ToxicityMeterController))]
    [RequireComponent(typeof(DetectionController))]
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(CharacterAnimator))]
    [RequireComponent(typeof(ToolSpawner))]
    [RequireComponent(typeof(CharacterRigAnchors))]
    [RequireComponent(typeof(CharacterVFXController))]
    [RequireComponent(typeof(CharacterReactions))]
    [RequireComponent(typeof(ModifierController))]
    [RequireComponent(typeof(CharacterWeaponScript))]
    [RequireComponent(typeof(CharacterMovementAgent))]
    [RequireComponent(typeof(CharacterMovementRigidbody))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CharacterVOController))]
    public class CharacterScript : MonoBehaviour
    {
        [Header("Character Subsystems")]
        [SerializeField] ToxicityMeterController toxicityController;
        [SerializeField] DetectionController detectionController;
        [SerializeField] CharacterMovement movementScript;
        [SerializeField] CharacterAnimator characterAnimator;
        [SerializeField] ToolSpawner toolSpawner;
        [SerializeField] CharacterRigAnchors characterRigAnchors;
        [SerializeField] CharacterVFXController vfxController;
        [SerializeField] CharacterReactions reactions;
        [SerializeField] ModifierController modifierController;
        [SerializeField] CharacterWeaponScript weaponScript;
        [SerializeField] CharacterMovementAgent agentModule;
        [SerializeField] CharacterMovementRigidbody rbModule;
        [SerializeField] NavMeshAgent agent;
        [SerializeField] Rigidbody rb;
        [SerializeField] CharacterVOController vOController;

        #region Properties
        public CharacterVOController VOController => vOController;
        public Rigidbody Rb => rb;
        public NavMeshAgent Agent => agent;
        public CharacterMovementAgent CharacterMovementAgent => agentModule;
        public CharacterMovementRigidbody CharacterMovementRigidbody => rbModule;
        public ToxicityMeterController ToxicityController => toxicityController;
        public DetectionController DetectionController => detectionController;
        public CharacterMovement MovementScript => movementScript;
        public CharacterAnimator Animator => characterAnimator;
        public ToolSpawner ToolSpawner => toolSpawner;
        public CharacterRigAnchors CharacterRigAnchors => characterRigAnchors;
        public CharacterVFXController VFXController => vfxController;
        public CharacterReactions Reactions => reactions;
        public ModifierController Modifier => modifierController;
        public CharacterWeaponScript WeaponScript => weaponScript;

        public int Index => index;
        public CharacterType Type => type;
        public CharacterRole Role => role;

        public IReadOnlyList<int> Levels => levels;
        public IReadOnlyList<ItemSubType> Traits => traits;
        public IReadOnlyDictionary<CustSubType, ItemSubType> CustItemsEquiped => custItemsEquiped;
        public IReadOnlyDictionary<ItemSubType, int> Tools => tools;
        public IReadOnlyDictionary<ItemSubType, int> Weapons => weapons;

        public bool IgnoreWeapons => ignoreWeapons;
        public bool Testing => isTesting;
        #endregion

        #region Fields
        protected int index;
        protected CharacterRole role;
        protected CharacterType type;

        protected List<int> levels;
        protected List<ItemSubType> traits;
        protected Dictionary<CustSubType, ItemSubType> custItemsEquiped;
        protected Dictionary<ItemSubType, int> tools;
        protected Dictionary<ItemSubType, int> weapons;

        bool ignoreWeapons;
        bool isTesting;
        #endregion


        #region Initialization
        /// <summary>
        /// Initializes the character with the provided data package.
        /// </summary>
        public virtual void Init(CharacterInitializationData initData)
        {
            isTesting = GameConfig.Instance.GameType == GameType.TestingLevel;
            index = initData.Index;
            role = initData.Role;
            type = initData.Type;
            levels = initData.Levels;
            traits = initData.Traits;
            custItemsEquiped = initData.CustItemsEquiped;
            tools = initData.Tools;
            weapons = initData.Weapons;
            ignoreWeapons = initData.ignoreWeapons;

            SetRole(role);
        }

        /// <summary>
        /// Sets up all character subsystems based on role (Main Player or NPC).
        /// </summary>
        protected virtual void SetRole(CharacterRole role)
        {
            toxicityController.Init(this);
            reactions.Init(this);
            weaponScript.Init(this);
            movementScript.Init(this);
            characterAnimator.Init(this);
            toolSpawner.Init(this);
            vfxController.Init(this);
            modifierController.Init(this);
            vOController.Init(this);

            // Role-specific detection range configuration
            if (role == CharacterRole.MainPlayer)
                detectionController.Init(CharactersConfig.Instance.MainCharDetectionOnEnter, CharactersConfig.Instance.MainCharDetectionOnExit, this);
            else
                detectionController.Init(CharactersConfig.Instance.SideCharDetectionOnEnter, CharactersConfig.Instance.SideCharDetectionOnExit, this);

            // Apply modifiers slightly delayed to ensure all equipment is initialized
            LeanTween.delayedCall(0.05f, () =>
            {
                modifierController.InitEquippement(levels, custItemsEquiped, traits);
            });
        }
        #endregion
    }
}
