using ClumsyCrew.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Handles runtime split-screen setup and RenderTexture assignment.
    /// - Dynamically creates and assigns render targets for each player.
    /// - Adjusts RawImage viewport layout for 1–4 player configurations.
    /// - Manages UI camera assignment and game HUD instantiation.
    /// </summary>
    public class RTController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RawImage[] rawImages;
        [SerializeField] RenderTexture[] renderTextures;
        [SerializeField] GameObject horizontalSeparationLine;
        [SerializeField] GameObject verticalSeparationLine;
        [SerializeField] RectTransform minigamePlayersHud;
        [SerializeField] RectTransform minigameGameHud;

        public RawImage[] RawImages => rawImages;
        public Transform HudTrans => minigamePlayersHud;
        public GameHudController GameHudController => gameHudController;

        GameHudController gameHudController;

        #region Initialization
        void Start()
        {
            GameManager.Instance.GameStateChanged += GameStateChanged;

            // Assign UI camera for world canvas
            GetComponent<Canvas>().worldCamera = SceneManager.Instance.CameraController.CamerasDict[CamType.UI];

            // Spawn Game HUD controller for current scene
            gameHudController = Instantiate(PrefabsConfig.Instance.GameHudController, minigameGameHud);
        }

        /// <summary>
        /// Handles HUD and split-screen updates when game state changes.
        /// </summary>
        void GameStateChanged(GameState state)
        {
            if (state == GameState.LevelEntered)
                gameHudController = Instantiate(PrefabsConfig.Instance.GameHudController, minigameGameHud);

            if (state == GameState.Menu)
            {
                DisableAllViews();
                Destroy(gameHudController.gameObject);
            }
        }
        #endregion


        #region Render Texture Setup
        /// <summary>
        /// Assigns a player's camera to a render texture slot and updates layout if needed.
        /// </summary>
        public void AssignCameraToRT(PlayerSlot slot, bool update)
        {
            int index = slot.index;

            verticalSeparationLine.SetActive(index > 0);
            horizontalSeparationLine.SetActive(index > 1);

            if (update)
                UpdateCurrentRTs(index + 1);

            slot.characterCamera.MainCam.targetTexture = renderTextures[index];
        }

        /// <summary>
        /// Updates RenderTexture sizes and RawImage layout based on active player count.
        /// </summary>
        void UpdateCurrentRTs(int numb)
        {
            Vector2 resolutionScale = Vector2.one;
            if (numb == 2)
                resolutionScale = new Vector2(0.5f, 1);
            else if (numb == 3 || numb == 4)
                resolutionScale = new Vector2(0.5f, 0.5f);

            int width = Mathf.RoundToInt(Screen.width * resolutionScale.x);
            int height = Mathf.RoundToInt(Screen.height * resolutionScale.y);

            for (int i = 0; i < rawImages.Length; i++)
            {
                rawImages[i].gameObject.SetActive(numb > i);
                if (i < numb)
                {
                    SetRawImageSizeAndPos(i, numb);
                    renderTextures[i].Release();
                    renderTextures[i].width = width;
                    renderTextures[i].height = height;
                    renderTextures[i].Create();
                }
            }
        }

        /// <summary>
        /// Assigns multiple player cameras to RenderTextures, updates layout, and clears buffers.
        /// </summary>
        public void AssignCamerasToRTs(List<PlayerSlot> slots)
        {
            verticalSeparationLine.SetActive(true);
            if (slots.Count > 2)
                horizontalSeparationLine.SetActive(true);

            Vector2 resolutionScale = Vector2.one;
            if (slots.Count == 2)
                resolutionScale = new Vector2(0.5f, 1);
            else if (slots.Count == 3 || slots.Count == 4)
                resolutionScale = new Vector2(0.5f, 0.5f);

            int width = Mathf.RoundToInt(Screen.width * resolutionScale.x);
            int height = Mathf.RoundToInt(Screen.height * resolutionScale.y);

            for (int i = 0; i < rawImages.Length; i++)
            {
                rawImages[i].gameObject.SetActive(slots.Count > i);
                if (i < slots.Count)
                {
                    SetRawImageSizeAndPos(slots[i].index, slots.Count);

                    renderTextures[i].Release();
                    renderTextures[i].width = width;
                    renderTextures[i].height = height;
                    renderTextures[i].Create();

                    // Assign texture and force buffer refresh
                    var cam = slots[i].characterCamera.MainCam;
                    cam.targetTexture = null;
                    cam.targetTexture = renderTextures[i];

                    // Clear initial frame
                    RenderTexture.active = renderTextures[i];
                    GL.Clear(true, true, Color.clear);
                    RenderTexture.active = null;
                }
            }
        }
        #endregion


        #region Utility
        /// <summary>
        /// Disables all RawImages and resets separation lines.
        /// </summary>
        public void DisableAllViews()
        {
            foreach (var img in rawImages)
                img.gameObject.SetActive(false);

            foreach (var rt in renderTextures)
                Graphics.SetRenderTarget(rt);

            verticalSeparationLine.SetActive(false);
            horizontalSeparationLine.SetActive(false);
        }

        /// <summary>
        /// Adjusts the RawImage anchor and size for a given player count configuration.
        /// </summary>
        void SetRawImageSizeAndPos(int playerIndex, int totalPlayers)
        {
            RectTransform rt = rawImages[playerIndex].GetComponent<RectTransform>();

            if (totalPlayers == 2)
            {
                // Vertical split
                rt.anchorMin = new Vector2(playerIndex == 0 ? 0f : 0.5f, 0f);
                rt.anchorMax = new Vector2(playerIndex == 0 ? 0.5f : 1f, 1f);
                rt.sizeDelta = Vector2.zero;
            }
            else if (totalPlayers == 3 || totalPlayers == 4)
            {
                // 4-quadrant layout (each 50% of screen)
                float xMin = (playerIndex == 1 || playerIndex == 3) ? 0.5f : 0f;
                float yMin = (playerIndex == 2 || playerIndex == 3) ? 0f : 0.5f;

                rt.anchorMin = new Vector2(xMin, yMin);
                rt.anchorMax = new Vector2(xMin + 0.5f, yMin + 0.5f);
                rt.sizeDelta = Vector2.zero;
            }
        }
        #endregion
    }
}
