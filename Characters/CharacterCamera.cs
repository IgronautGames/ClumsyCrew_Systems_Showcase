using ClumsyCrew.Core;
using ClumsyCrew.Minigames;
using MoreMountains.Feedbacks;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Player camera controller that integrates Cinemachine, Feel feedbacks, and camera shake effects.
    /// Handles dynamic zooms, framing shifts, and impulse reactions during gameplay.
    /// </summary>
    public class CharacterCamera : MonoBehaviour
    {
        [Header("Cinemachine References")]
        [SerializeField] Camera mainCam;
        [SerializeField] CinemachineCamera cam;
        [SerializeField] CinemachinePositionComposer composer;
        [SerializeField] CinemachineGroupFraming groupFramer;
        [SerializeField] CinemachineTargetGroup targetGroup;

        [Header("Feedback & Audio")]
        [SerializeField] FeelItem[] feelItems;
        [SerializeField] AudioListener audioListener;
        [SerializeField] CinemachineImpulseSource impulseSource;
        [SerializeField] CinemachineImpulseListener impulseListener;

        [Header("Camera Smoothing")]
        [SerializeField] float offsetAmountX = 0.1f;
        [SerializeField] float offsetAmountY = 0.1f;
        [SerializeField] float smoothSpeed = 3f;
        [SerializeField] float targetGroupWeight = 1;
        [SerializeField] float targetGroupRadius = 4;
        [SerializeField] float normalCameraZoom = 0.7f;

        MainCharacterScript mainChar;
        Transform playerTrans;
        Vector3 lastDirection;
        const float movementThreshold = 0.01f;

        Dictionary<FeedbackType, MMFeedbacks> feelDict = new();
        IReadOnlyDictionary<CameraShakeType, CameraShakeDefinition> cameraShakeDict;
        int index;

        #region Properties
        public CinemachineTargetGroup TargetGroup => targetGroup;
        public Camera MainCam => mainCam;
        public CinemachineCamera VCam => cam;
        #endregion


        #region Initialization
        void Awake()
        {
            cameraShakeDict = DefinitionsManager.Instance.CameraShakesDict;
            GameManager.Instance.GameStateChanged += GameStateChanged;

            // Cache Feel feedback dictionary
            for (int i = 0; i < feelItems.Length; i++)
                feelDict.Add(feelItems[i].type, feelItems[i].feedback);

            // Remove redundant AudioListener in multiplayer
            if (GameManager.Instance.CurrentJobDef.MinigameType == MinigameType.LocalMultiplayer)
                Destroy(audioListener);

            // Default zoom level
            if (GameConfig.Instance.GameType != GameType.TestingLevel)
                ZoomMainCamera(0.55f, 3f);
        }

        void Start()
        {
            mainChar.WeaponScript.OnWeaponTriggered += OnWeaponTriggered;
            mainChar.Reactions.OnReactionChanged += OnReactionChanged;
        }

        void OnDestroy()
        {
            GameManager.Instance.GameStateChanged -= GameStateChanged;
        }
        #endregion


        #region Event Listeners
        void GameStateChanged(GameState gameState)
        {
            if (gameState == GameState.Minigame)
                ZoomMainCamera(normalCameraZoom, 0.5f);
        }

        void OnWeaponTriggered(WeaponItem weapon, CharacterScript character)
        {
            if (weapon.Def.TriggeringCameraFeedback)
                PlayImpulse(weapon.Def.TriggerCameraFeedbackType);
        }

        void OnReactionChanged(ReactionDef def, int playerNumb)
        {
            if (def.HasCameraImpulse)
                PlayImpulse(def.CameraImpulse);
        }
        #endregion


        #region Update Loop
        void Update()
        {
            Vector3 moveDirection = playerTrans.forward;

            if (moveDirection.sqrMagnitude > movementThreshold)
                lastDirection = moveDirection;

            float targetScreenX = -lastDirection.x * offsetAmountX;
            float targetScreenY = lastDirection.z * offsetAmountY;

            var comp = composer.Composition;
            comp.ScreenPosition.x = Mathf.Lerp(comp.ScreenPosition.x, targetScreenX, Time.deltaTime * smoothSpeed);
            comp.ScreenPosition.y = Mathf.Lerp(comp.ScreenPosition.y, targetScreenY, Time.deltaTime * smoothSpeed);
            composer.Composition = comp;
        }
        #endregion


        #region Public Setup
        public void Init(int index)
        {
            impulseSource.ImpulseDefinition.ImpulseChannel = 1 << index;
            impulseListener.ChannelMask = 1 << index;
            this.index = index;
        }

        public void Init(MainCharacterScript mainChar)
        {
            playerTrans = mainChar.transform;
            this.mainChar = mainChar;
            mainChar.ToolUseController.OnChargeDone += OnChargeDone;
        }
        #endregion


        #region Target & Zoom Control
        public void ChangeTarget(Transform target)
        {
            targetGroup.Targets = new List<CinemachineTargetGroup.Target>();
            targetGroup.AddMember(target, targetGroupWeight, targetGroupRadius);
        }

        public void MoveTargetToObject(Transform trans, float transitionTime)
        {
            targetGroup.AddMember(trans, 0, 3);
            LeanTween.value(gameObject, 1f, 0f, transitionTime).setOnUpdate(val =>
            {
                targetGroup.Targets[0].Weight = val;
            });
            LeanTween.value(gameObject, 0f, 1f, transitionTime).setOnUpdate(val =>
            {
                targetGroup.Targets[1].Weight = val;
            });
        }

        public void MoveTargetToMainPlayer(Transform trans, float transitionTime)
        {
            LeanTween.value(gameObject, 0, 1, transitionTime).setOnUpdate(val =>
            {
                targetGroup.Targets[0].Weight = val;
            });
            LeanTween.value(gameObject, 1, 0, transitionTime).setOnUpdate(val =>
            {
                targetGroup.Targets[1].Weight = val;
            }).setOnComplete(() =>
            {
                targetGroup.RemoveMember(trans);
            });
        }

        public void ZoomMainCamera(float zoomAmount, float time)
        {
            float currentZoom = groupFramer.FramingSize;
            LeanTween.cancel(groupFramer.gameObject);
            LeanTween.value(groupFramer.gameObject, currentZoom, zoomAmount, time)
                .setOnUpdate(val => groupFramer.FramingSize = val);
        }

        public void ZoomOutThenZoomIn(float zoomAmount, float totalDur, float zoomOutDur)
        {
            ZoomMainCamera(zoomAmount, zoomOutDur);
            LeanTween.delayedCall(zoomOutDur, () =>
            {
                ZoomMainCamera(normalCameraZoom, totalDur - zoomOutDur);
            });
        }
        #endregion


        #region Feedback / Impulse
        public void PlayImpulse(CameraShakeType type)
        {
            CameraShakeUtility.Shake(impulseSource, cameraShakeDict[type], index);
        }

        void OnChargeDone(ToolUseType type)
        {
            if (type == ToolUseType.Good)
                ZoomOutThenZoomIn(0.35f, 0.25f, 0.3f);
            else if (type == ToolUseType.Perfect)
                ZoomOutThenZoomIn(0.25f, 0.35f, 0.4f);
        }
        #endregion
    }
}
