using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ClumsyCrew.UI;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Dynamic joystick that appears wherever the player presses the screen.
    /// Works with both:
    /// - EnhancedTouch (mobile)
    /// - Mouse input (editor / PC)
    ///
    /// Includes:
    /// - UI raycast protection
    /// - Clickable world detection (prevents joystick from activating over interactables)
    /// - External pointer tracking for UI elements pressed during gameplay
    /// </summary>
    public class DynamicJoystick : MonoBehaviour
    {
        [SerializeField] GameObject joystickBackground;
        [SerializeField] RectTransform joystickHandle;
        [SerializeField] RectTransform joystickRect;
        [SerializeField] float joystickRadius = 100f;

        Vector2 inputDirection = Vector2.zero;
        bool isDragging;
        Vector2 starPos;
        bool ignoreInput;
        bool isHeldDown;
        int joystickFingerId = -1;
        float halfWidth;

        static readonly List<RaycastResult> uiHits = new();

        // External pointer tracking (for UI handoff)
        int watchedPointerId = int.MinValue;
        GameObject watchedTarget;
        bool pendingBegin;
        int pendingFingerId = -1;
        Vector2 pendingPos;
        int watchedFingerIndex = -1;
        int watchArmedFrame = -1;

        public PlayerInputActions inputActions;
        public bool ignoreOverUI;
        public bool IsHeldDown => isHeldDown;

        void Awake()
        {
            inputActions = new PlayerInputActions();
            inputActions.Enable();

            inputActions.Player.MouseClick.started += OnMouseClickStarted;
            inputActions.Player.MouseClick.canceled += OnMouseClickCanceled;

            EnhancedTouchSupport.Enable();
        }
        void Start()
        {
            joystickRect.gameObject.SetActive(false);
            halfWidth = Screen.width * 0.5f;
        }
        void OnDisable()
        {
            OnTouchUp();
        }
        void Update()
        {
#if UNITY_EDITOR
            if (isDragging)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                OnDrag(mousePosition);
            }
#else
            HandleTouchInput();
#endif
        }

        #region Touch Input
        void HandleTouchInput()
        {
            var touches = Touch.activeTouches;
            bool watchedFingerStillActive = false;

            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];

                // Defer "Begin" decision for one frame
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began && joystickFingerId == -1)
                {
                    isHeldDown = true;
                    if (t.startScreenPosition.x < halfWidth)
                    {
                        pendingBegin = true;
                        pendingFingerId = t.finger.index;
                        pendingPos = t.screenPosition;
                    }
                }

                // Joystick tracking
                if (joystickFingerId != -1 && t.finger.index == joystickFingerId)
                {
                    if (t.phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                        t.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                    {
                        OnDrag(t.screenPosition);
                    }
                    else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                             t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        OnTouchUp();
                        joystickFingerId = -1;
                    }
                }

                // Watch for external UI presses
                if (watchedTarget != null && watchedFingerIndex >= 0 && t.finger.index == watchedFingerIndex)
                {
                    watchedFingerStillActive = true;

                    if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                        t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        FireUpToWatchedTarget(t.screenPosition);
                    }
                }
            }

            // Process deferred begin once per frame (after UI updates)
            if (pendingBegin)
            {
                pendingBegin = false;
                bool overUI = !ignoreOverUI && IsOverUI(pendingPos);
                bool overClickable = IsOverClickableWorld(pendingPos);

                if (!overUI && !overClickable && joystickFingerId == -1)
                {
                    joystickFingerId = pendingFingerId;
                    starPos = pendingPos;
                    OnTouchDown(pendingPos);
                }
            }

            // Safety check: only after at least one frame since arming
            if (watchedTarget != null && Time.frameCount > watchArmedFrame)
            {
                if (watchedFingerIndex >= 0 && !watchedFingerStillActive)
                    FireUpToWatchedTarget(Vector2.zero);
            }
        }
        #endregion

        #region Mouse Input
        void OnMouseClickStarted(InputAction.CallbackContext context)
        {
            var pos = Mouse.current.position.ReadValue();
            starPos = pos;
            isHeldDown = true;

            if (!ignoreOverUI && IsOverUI(pos)) return;
            if (IsOverClickableWorld(pos)) return;

            joystickFingerId = int.MaxValue;
            OnTouchDown(pos);
        }
        void OnMouseClickCanceled(InputAction.CallbackContext context)
        {
            if (joystickFingerId == int.MaxValue)
                joystickFingerId = -1;

            OnTouchUp();

            // Force pointerUp forward too (same as touch end)
            if (watchedTarget != null)
                FireUpToWatchedTarget(Mouse.current.position.ReadValue());
        }
        #endregion

        #region Joystick Logic
        void OnPointerDown(Vector2 pointerPosition)
        {
            isDragging = true;
            joystickRect.gameObject.SetActive(true);
            joystickRect.anchoredPosition = UIManager.Instance.GetNormlizedScreenCoordinatesScreenPos(pointerPosition);
        }
        void OnDrag(Vector2 pointerPosition)
        {
            if (ignoreInput)
                return;

            Vector2 direction = UIManager.Instance.GetNormlizedScreenCoordinatesScreenPos(pointerPosition)
                              - joystickRect.anchoredPosition;

            float distance = Mathf.Clamp(direction.magnitude, 0, joystickRadius);
            joystickHandle.anchoredPosition = direction.normalized * distance;
            inputDirection = direction / joystickRadius;
        }
        void OnTouchDown(Vector2 touchPosition)
        {
            OnPointerDown(touchPosition);
        }
        void OnTouchUp()
        {
            isHeldDown = false;
            ignoreInput = false;
            isDragging = false;
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickRect.gameObject.SetActive(false);
            inputDirection = Vector2.zero;
        }
        public void ForceCancelAllPointers()
        {
            if (isDragging || joystickFingerId != -1)
            {
                OnTouchUp();
                joystickFingerId = -1;
            }
        }
        #endregion

        #region Helpers & Watch System
        void FireUpToWatchedTarget(Vector2 pos)
        {
            var up = new PointerEventData(EventSystem.current)
            {
                pointerId = watchedPointerId,
                position = pos,
                button = PointerEventData.InputButton.Left
            };

            ExecuteEvents.ExecuteHierarchy(watchedTarget, up, ExecuteEvents.pointerUpHandler);

            watchedPointerId = int.MinValue;
            watchedFingerIndex = -1;
            watchedTarget = null;
            watchArmedFrame = -1;
        }
        public Vector2 GetInputDirection() => inputDirection;
        public void JoystickSee(bool see)
        {
            joystickBackground.SetActive(see);
            joystickHandle.gameObject.SetActive(see);
        }
        public void StopJoystick()
        {
            OnTouchUp();
        }
        bool IsOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screenPos };
            uiHits.Clear();
            EventSystem.current.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }
        bool IsOverClickableWorld(Vector2 screenPos)
        {
            var ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.collider.CompareTag("JobItem") || hit.collider.CompareTag("Pickable");
            }
            return false;
        }
        public void WatchExternalPointerRelease(int pointerId, Vector2 uiPos, GameObject targetButtonGO)
        {
            watchedPointerId = pointerId;
            watchedTarget = targetButtonGO;
            watchedFingerIndex = FindMatchingFingerIndex(uiPos);
            watchArmedFrame = Time.frameCount;
        }
        int FindMatchingFingerIndex(Vector2 pos)
        {
            var touches = Touch.activeTouches;
            int best = -1; float bestSqr = float.MaxValue;

            for (int i = 0; i < touches.Count; i++)
            {
                float d = (touches[i].screenPosition - pos).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = touches[i].finger.index; }
            }

            if (bestSqr > 80f * 80f) return -1;
            return best;
        }
        #endregion
    }
}
