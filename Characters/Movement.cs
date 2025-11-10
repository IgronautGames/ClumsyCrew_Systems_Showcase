using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

using ClumsyCrew.Core;
using ClumsyCrew.UI;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Handles character movement across multiple modes:
    /// - NavMeshAgent based (CharacterMovementAgent)
    /// - CharacterController based (CharacterMovementCC)
    /// - Animation-only (root motion / reactions)
    ///
    /// Also abstracts input between:
    /// - Gamepad (console / PC editor)
    /// - On-screen dynamic joystick (mobile / touch)
    ///
    /// Speed is driven by MoveSpeed definitions (CharactersConfig)
    /// and modified by character modifiers (e.g. Speed buff/debuff).
    /// </summary>
    public class Movement : MonoBehaviour
    {
        #region Fields

        // Input
        DynamicJoystickScript joystick;
        Gamepad gamepad;

        // Core refs
        protected CharacterAnimator animator;
        protected CharacterScript character;
        CharacterMovementAgent agentMovement;
        CharacterMovementCC ccMovement;
        CharacterMovementRigidbody rbMovement;
        protected CharacterRole role;

        // State / mode
        bool isConsole;                       // True if using gamepad instead of on-screen joystick
        MovementsCommandMode commandMode;    // Full control, rotation-only, or none
        protected MovementMode movementMode;         // Agent, CharacterController, Animation, None

        // Input cache
        Vector2 joystickInput;
        Vector3 moveDirection;

        // Animator velocity smoothing (for cleaner network sync)
        float animatorSmoothSpeed;
        float animatorSmoothVelocity;

        // Speed
        readonly Dictionary<MoveSpeed, SpeedValue> speedsDict = new();
        MoveSpeed currentSpeedType;
        float currentSpeed;
        float targetSpeed;
        float speedModifier;
        readonly float speedChangingMultiplier = 10f;
        bool agentInteruptable = true;

        #endregion

        #region Properties

        public MovementMode MovementMode => movementMode;
        public float CurrentSpeed => currentSpeed;
        public CharacterMovementAgent AgentMovement => agentMovement;
        public CharacterMovementCC CCMovement => ccMovement;

        #endregion

        #region Init & Lifetime
        /// <summary>
        /// Injects the character reference after construction.
        /// Call this once right after instantiation.
        /// </summary>
        public void Init(CharacterScript character)
        {
            this.character = character;
        }
        void Awake()
        {
            // Cache speed definitions into a dictionary for quick lookup
            var config = CharactersConfig.Instance;
            for (int i = 0; i < config.Speeds.Length; i++)
            {
                var def = config.Speeds[i];
                speedsDict.Add(def.movementSpeedType, def);
            }
        }
        void Start()
        {
            animator = character.Animator;
            role = character.Role;

            // In this project "console" means we use a Gamepad instead of on-screen joystick.
            isConsole = PlatformManager.CurrentGrupped == GamePlatform.PCEditor;

            agentMovement = character.CharacterMovementAgent;
            agentMovement.Init(character);

            rbMovement = character.CharacterMovementRigidbody;

            // Listen for modifier changes (e.g. speed buffs)
            character.Modifier.OnModifierChanged += ModifierChanged;
            speedModifier = character.Modifier.ModifiedStats[CharStatType.Speed] / 100f;

            if (role == CharacterRole.NPC)
            {
                // NPCs are driven by NavMeshAgent logic
                movementMode = MovementMode.Agent;
                agentMovement.AgentSetActive(true);
            }
            else
            {
                // Player-controlled character
                ccMovement = ((MainCharacterScript)character).CharacterMovementControllerCC;

                GameManager.Instance.GameStateChanged += GameStateChange;
                animator.OnChangeMovementToAnim += OnChangeMovementToAnim;

                if (isConsole)
                    gamepad = GameManager.Instance.Players.ActivePads[character.Index];
                else
                    joystick = UIManager.Instance.DynamicJoystick;

                SwitchMode(MovementMode.None);
                character.Reactions.OnReactionChanged += OnReactionChanged;

                // In testing levels we directly jump into minigame state
                if (GameConfig.Instance.GameType == GameType.TestingLevel)
                    GameStateChange(GameState.Minigame);
            }
        }
        void OnDestroy()
        {
            if (role == CharacterRole.MainPlayer)
                GameManager.Instance.GameStateChanged -= GameStateChange;
        }
        #endregion

        #region Listeners
        void GameStateChange(GameState state)
        {
            if (state == GameState.Minigame)
            {
                // For gamepad we use CharacterController, for touch we use Agent
                SwitchMode(isConsole ? MovementMode.CharacterController : MovementMode.Agent);
            }
            else if (state == GameState.MinigameEnd)
            {
                SwitchMode(MovementMode.None);
            }
        }
        void ModifierChanged(ModifierType type)
        {
            if (type != ModifierType.Speed)
                return;

            speedModifier = character.Modifier.ModifiedStats[CharStatType.Speed] / 100f;
            UpdateTargetSpeed(currentSpeedType);
        }
        void OnChangeMovementToAnim(bool toAnim)
        {
            if (toAnim)
                SwitchMode(MovementMode.Animation);
            else
                SwitchMode(isConsole ? MovementMode.CharacterController : MovementMode.Agent);
        }
        void OnReactionChanged(ReactionDef def, int playerNumb)
        {
            // When character is falling / ragdolling, we switch to animation-only movement
            if (def.Type == ReactionType.Falling)
                SwitchMode(MovementMode.Animation);
        }
        #endregion

        #region Update Loop
        protected virtual void Update()
        {
            UpdateSpeed();

            // Drive animator velocity from whichever movement source is active
            if (movementMode == MovementMode.Agent)
                UpdateAnimatorVelocity(agentMovement.AgentVelocity);
            else if (movementMode == MovementMode.CharacterController)
                UpdateAnimatorVelocity(ccMovement.CurrentVelocity);
            else
                UpdateAnimatorVelocity(0f);

            // Main player reads input and commands movement
            if (role == CharacterRole.MainPlayer &&
                (movementMode == MovementMode.Agent || movementMode == MovementMode.CharacterController))
            {
                UpdateJoystick();
            }
        }
        #endregion

        #region Input Handling
        void UpdateJoystick()
        {
            // If no commands are allowed, ensure speed goes to zero.
            if (commandMode == MovementsCommandMode.None)
            {
                if (currentSpeedType != MoveSpeed.None)
                    UpdateTargetSpeed(MoveSpeed.None);
                return;
            }

            // Full control → we want running speed by default
            if (commandMode == MovementsCommandMode.Full && currentSpeedType != MoveSpeed.Run)
            {
                UpdateTargetSpeed(MoveSpeed.Run);
            }
            else if (commandMode == MovementsCommandMode.Rotation && currentSpeedType != MoveSpeed.None)
            {
                UpdateTargetSpeed(MoveSpeed.None);
            }

            if (isConsole)
            {
                // Gamepad stick input
                joystickInput = gamepad.leftStick.ReadValue();
                moveDirection = new Vector3(joystickInput.x, 0f, joystickInput.y).normalized;

                if (commandMode == MovementsCommandMode.Full)
                {
                    ccMovement.MoveCC(moveDirection);
                }
                else if (joystickInput.sqrMagnitude > 0.05f)
                {
                    LookAt(transform.position + moveDirection);
                }
            }
            else
            {
                // On-screen dynamic joystick input
                joystickInput = joystick.GetInputDirection();
                if (joystickInput == Vector2.zero)
                    return;

                float intensity = Mathf.Clamp01(joystickInput.sqrMagnitude);
                if (intensity <= 0.1f)
                    return;

                moveDirection = new Vector3(joystickInput.x, 0f, joystickInput.y).normalized;

                if (commandMode == MovementsCommandMode.Full)
                {
                    // Agent is commanded by "click to move" style destinations
                    Vector3 destination = transform.position + moveDirection * intensity;
                    agentMovement.MoveAgent(destination);
                }
                else
                {
                    LookAt(transform.position + moveDirection);
                }
            }
        }
        #endregion

        #region Speed Handling
        void UpdateSpeed()
        {
            if (Mathf.Abs(currentSpeed - targetSpeed) <= 0.01f)
                return;

            // Smoothly approach target speed
            if (currentSpeed > targetSpeed)
                currentSpeed -= Time.deltaTime * speedChangingMultiplier;
            else
                currentSpeed += Time.deltaTime * speedChangingMultiplier;

            // Apply to active movement implementation
            if (movementMode == MovementMode.Agent)
                agentMovement.UpdateAgentSpeed(currentSpeed);
            else if (movementMode == MovementMode.CharacterController)
                ccMovement.UpdateCCSpeed(currentSpeed);
        }
        void UpdateTargetSpeed(MoveSpeed speed)
        {
            currentSpeedType = speed;

            if (speed == MoveSpeed.None)
            {
                targetSpeed = 0f;
                return;
            }

            // Base speed is defined in CharactersConfig, then scaled by speedModifier
            SpeedValue def = speedsDict[currentSpeedType];
            float baseSpeed = def.speed;
            float modifierFactor = Mathf.Lerp(0.2f, 1f, speedModifier);

            targetSpeed = baseSpeed * modifierFactor;
        }
        void UpdateAnimatorVelocity(float currentVelocity)
        {
            // If you want smoother visual updates, use animatorSmoothSpeed instead:
            // animatorSmoothSpeed = Mathf.SmoothDamp(
            //     animatorSmoothSpeed,
            //     currentVelocity,
            //     ref animatorSmoothVelocity,
            //     0.1f);

            // animator.SetVelocity(animatorSmoothSpeed);
            animator.SetVelocity(currentVelocity);
        }
        #endregion

        #region Mode Switching
        void SwitchMode(MovementMode mode)
        {
            // Prevent interrupting a non-interruptible agent command
            if ((mode == MovementMode.CharacterController || mode == MovementMode.Animation) && !agentInteruptable)
                return;

            bool inPlay =
                GameManager.Instance.CurrentGameState == GameState.Minigame ||
                GameManager.Instance.CurrentGameState == GameState.Chasing;

            // Decide what kind of input commands are allowed in this mode
            if ((!isConsole && mode == MovementMode.Agent) || (inPlay && mode == MovementMode.CharacterController))
            {
                commandMode = MovementsCommandMode.Full;
            }
            else if (mode == MovementMode.Animation && animator.IsInAttack)
            {
                // During attack animations we might allow only rotation to face the target
                commandMode = MovementsCommandMode.Rotation;
            }
            else
            {
                commandMode = MovementsCommandMode.None;
            }

            // Toggle movement implementations
            agentMovement.AgentSetActive(mode == MovementMode.Agent);
            ccMovement.EnableDisableController(mode == MovementMode.CharacterController);

            movementMode = mode;

            // Show/hide on-screen joystick depending on whether we can send commands
            if (!isConsole && joystick != null)
                joystick.gameObject.SetActive(commandMode != MovementsCommandMode.None);
        }
        #endregion

        #region Public Helpers
        /// <summary>
        /// Rotates the character to look at a target position on the XZ plane.
        /// </summary>
        public void LookAt(Vector3 pos)
        {
            transform.LookAt(new Vector3(pos.x, transform.position.y, pos.z));
        }
        /// <summary>
        /// Teleports the character to a given position and safely resets
        /// NavMeshAgent / CharacterController to avoid glitches.
        /// </summary>
        public void SetPos(Vector3 pos)
        {
            if (movementMode == MovementMode.Agent)
            {
                agentMovement.AgentSetActive(false);
                LeanTween.delayedCall(0.01f, () =>
                {
                    agentMovement.AgentSetActive(true);
                });
            }

            if (movementMode == MovementMode.CharacterController)
            {
                ccMovement.EnableDisableController(false);
                movementMode = MovementMode.None;

                LeanTween.delayedCall(0.01f, () =>
                {
                    ccMovement.EnableDisableController(true);
                    movementMode = MovementMode.CharacterController;
                });
            }

            transform.position = pos;
        }
        /// <summary>
        /// Sends the character to a world position using the NavMeshAgent and,
        /// once the destination is reached (or timed out), returns control back
        /// to the previous movement mode.
        ///
        /// Useful for scripted sequences, cutscenes, or auto-move behaviour.
        /// </summary>
        /// <param name="pos">Destination world position.</param>
        /// <param name="DoneAction">Callback invoked when the agent finishes (bool reached).</param>
        /// <param name="speed">Speed type to use while moving.</param>
        /// <param name="stopDistance">Stopping distance.</param>
        /// <param name="interruptable">If false, player input cannot break this command.</param>
        /// <param name="timeout">Failsafe timeout in seconds.</param>
        public void SendAgentAndReturn(
            Vector3 pos,
            UnityAction<bool> DoneAction = default,
            MoveSpeed speed = MoveSpeed.Run,
            float stopDistance = 0f,
            bool interruptable = true,
            float timeout = 10f)
        {
            if (role == CharacterRole.MainPlayer && isConsole)
                SwitchMode(MovementMode.Agent);

            if (currentSpeedType != speed)
                UpdateTargetSpeed(speed);

            agentInteruptable = interruptable;

            agentMovement.SendAgent(pos, reached =>
            {
                agentInteruptable = true;

                if (role == CharacterRole.MainPlayer && isConsole)
                    SwitchMode(MovementMode.CharacterController);

                DoneAction?.Invoke(reached);

            }, stopDistance, interruptable, timeout);
        }

        #endregion
    }
}
