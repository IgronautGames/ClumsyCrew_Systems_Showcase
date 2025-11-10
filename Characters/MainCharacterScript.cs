using ClumsyCrew.Core;
using ClumsyCrew.Minigames;
using ClumsyCrew.UI;
using System.Collections.Generic;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Main controllable character class.
    /// Inherits from <see cref="CharacterScript"/> and extends it with:
    /// - Player input and camera control
    /// - HUD management (console vs mobile)
    /// - Tool, item, and carrying systems
    /// - Platform-specific setup logic
    /// </summary>
    public class MainCharacterScript : CharacterScript
    {
        [Header("Character Visuals & Systems")]
        [SerializeField] CharMeshes[] chars;
        [SerializeField] HittingController hittingController;
        [SerializeField] CarryingLogic carryingLogic;
        [SerializeField] CarryingVisuals carryingVisuals;
        [SerializeField] TemporaryItemsController temporaryItemsController;
        [SerializeField] ToolController toolController;
        [SerializeField] ToolUsingController toolUseController;
        [SerializeField] ToolDamageCalculator toolDamageCalculator;
        [SerializeField] InteractionController interactionController;
        [SerializeField] StabilityMeterController stabilityMeterController;
        [SerializeField] ToolUsingHud toolUsingHud;
        [SerializeField] CharacterMovementCC ccModule;

        #region Properties
        public CharacterMovementCC CharacterMovementControllerCC => ccModule;
        public HittingController HittingController => hittingController;
        public CarryingLogic CarryingLogic => carryingLogic;
        public CarryingVisuals CarryingVisuals => carryingVisuals;
        public TemporaryItemsController TemporaryItemsController => temporaryItemsController;
        public ToolController ToolController => toolController;
        public InteractionController InteractionController => interactionController;
        public ToolUsingController ToolUseController => toolUseController;
        public ToolDamageCalculator DamageCalculator => toolDamageCalculator;
        public StabilityMeterController StabilityMeterController => stabilityMeterController;
        public PlayerHudController HudController => hudController;
        public ToolUsingHud ToolUsingHud => toolUsingHud;
        public CharacterCamera Camera => charCamera;
        public PlayerSlot PlayerSlot => playerSlot;
        public List<ItemSubType> ConsoleWeapons => consoleWeapons;
        #endregion

        PlayerSlot playerSlot;
        PlayerHudController hudController;
        CharacterCamera charCamera;
        readonly List<ItemSubType> consoleWeapons = new();

        #region Initialization
        /// <summary>
        /// Initializes the main character, camera, weapons, and platform-dependent HUD.
        /// </summary>
        public override void Init(CharacterInitializationData initData)
        {
            charCamera = initData.camera;
            base.Init(initData);
            charCamera.Init(this);

            // Randomly select console weapons
            int numbOfWeapons = initData.ConsoleWeapons.Count;
            for (int i = 0; i < numbOfWeapons; i++)
            {
                int index = Random.Range(0, initData.ConsoleWeapons.Count);
                ItemSubType type = initData.ConsoleWeapons[index];
                initData.ConsoleWeapons.RemoveAt(index);
                consoleWeapons.Add(type);
            }

            // Enable only meshes matching selected character type
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < chars[i].Meshes.Count; j++)
                    chars[i].Meshes[j].SetActive(chars[i].Type == initData.Type);
            }

            // Assign player slot if on PC
            if (PlatformManager.CurrentGrupped == GamePlatform.PCEditor)
                playerSlot = GameManager.Instance.Players.GetSlot(index);
        }

        /// <summary>
        /// Overrides base setup for player-specific systems (HUD, tools, carrying, etc.).
        /// </summary>
        protected override void SetRole(CharacterRole role)
        {
            base.SetRole(role);

            // Platform-specific HUD setup
            if (PlatformManager.CurrentGrupped == GamePlatform.Mobile)
            {
                PlayerHudControllerMobile mobileHud = Instantiate(PrefabsConfig.Instance.PlayerHudControllerMobile, UIManager.Instance.RtController.HudTrans);
                hudController = mobileHud;
                mobileHud.Init(this);
            }
            else
            {
                PlayerHudControllerConsole consoleHud = Instantiate(PrefabsConfig.Instance.PlayerHudControllerConsole, UIManager.Instance.RtController.HudTrans);
                hudController = consoleHud;
                consoleHud.Init(this);
            }

            // System initialization
            ToxicityController.Init(hudController.ToxicityHud, this);
            toolController.Init(this);
            toolUseController.Init(this);
            toolDamageCalculator.Init(this);
            interactionController.Init(this);
            stabilityMeterController.Init(this);
            toolUsingHud.Init(this);
            hittingController.Init(this);
            carryingLogic.Init(this);
            carryingVisuals.Init(this);
            temporaryItemsController.Init(this);
        }
        #endregion
    }
}
