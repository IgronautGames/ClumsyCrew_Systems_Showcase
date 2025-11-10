using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Manages all active players and connected gamepads.
    /// - Tracks active devices, assigns them to player slots, and handles reconnections.
    /// - Automatically updates input navigation and triggers slot events.
    /// - Supports both Menu and In-Game states with different assignment rules.
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public event Action<Gamepad> FirstPlayerChanged;
        public event UnityAction<PlayerSlot, bool> OnSlotChanged;

        readonly List<PlayerSlot> slots = new();
        readonly List<Gamepad> activePads = new();
        readonly Dictionary<Gamepad, float> connectedPads = new();

        float inactivityThreshold = 1f;

        #region Properties
        public List<Gamepad> ActivePads => activePads;
        public Gamepad FirstPlayer => activePads.Count > 0 ? activePads[0] : null;

        public PlayerSlot GetSlot(int index) => slots.Find(s => s.index == index);
        public List<PlayerSlot> GetActiveSlots() => slots.FindAll(s => s.active);
        public List<PlayerSlot> GetJoinedSlots() => slots.FindAll(s => s.joined);
        public List<PlayerSlot> GetConfirmedSlots() => slots.FindAll(s => s.confirmed);
        #endregion


        #region Initialization
        void Awake()
        {
            InputSystem.onDeviceChange += OnDeviceChange;

            // Register already-connected pads (useful when debugging or hot-reloading)
            foreach (var pad in Gamepad.all)
            {
                connectedPads.Add(pad, Time.unscaledTime);
                AssignInMenu(pad);
            }
        }
        #endregion


        #region Update Loop
        void Update()
        {
            float now = Time.unscaledTime;

            // Track pad activity and disconnect inactive ones
            foreach (var pad in Gamepad.all)
            {
                if (pad.wasUpdatedThisFrame)
                {
                    connectedPads[pad] = now;
                    if (!activePads.Contains(pad))
                        OnDeviceChange(pad, InputDeviceChange.Enabled);
                }

                if (activePads.Contains(pad) && now - connectedPads[pad] > inactivityThreshold)
                    OnDeviceChange(pad, InputDeviceChange.Disabled);
            }

            // Update all navigators
            foreach (var slot in slots)
            {
                if (slot.active)
                    slot.navigator.Update();
            }
        }
        #endregion


        #region Device Management
        void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is not Gamepad pad)
                return;

            Debug.Log($"Device {device.displayName} {change}");

            switch (change)
            {
                case InputDeviceChange.Added:
                    connectedPads.Add(pad, Time.unscaledTime);
                    DeviceActive(pad, true);
                    break;

                case InputDeviceChange.Enabled:
                    DeviceActive(pad, true);
                    break;

                case InputDeviceChange.Removed:
                    connectedPads.Remove(pad);
                    DeviceActive(pad, false);
                    break;

                case InputDeviceChange.Disabled:
                    DeviceActive(pad, false);
                    break;
            }
        }

        /// <summary>
        /// Activates or deactivates a device. Handles slot assignment and event dispatch.
        /// </summary>
        void DeviceActive(Gamepad pad, bool active)
        {
            if (active)
            {
                activePads.Add(pad);

                if (GameManager.Instance.CurrentGameState == GameState.Menu)
                    AssignInMenu(pad);
                else
                    AssignInGame(pad);

                if (activePads.Count == 1)
                    FirstPlayerChanged?.Invoke(pad);
            }
            else
            {
                bool wasFirst = activePads.Count > 0 && activePads[0] == pad;
                activePads.Remove(pad);

                if (GameManager.Instance.CurrentGameState == GameState.Menu)
                    RemoveInMenu(pad);
                else
                    DeactivateInGame(pad);

                if (wasFirst && activePads.Count > 0)
                    FirstPlayerChanged?.Invoke(activePads[0]);
            }
        }
        #endregion


        #region Slot Management (Menu)
        /// <summary>
        /// Assigns a connected pad to a new menu slot.
        /// </summary>
        void AssignInMenu(Gamepad pad)
        {
            if (slots.Exists(s => s.device == pad))
                return;

            slots.Add(new PlayerSlot
            {
                index = slots.Count,
                device = pad,
                active = true,
                characterSelected = CharacterType.Aljo,
                navigator = new(pad, slots.Count)
            });

            CompactSlots();
            OnSlotChanged?.Invoke(slots[^1], true);

            // Auto-confirm slots when testing
            if (GameConfig.Instance.GameType == GameType.TestingLevel)
            {
                slots[^1].confirmed = true;
                slots[^1].joined = true;
            }
        }

        /// <summary>
        /// Removes a pad from menu slot list and compacts remaining indices.
        /// </summary>
        void RemoveInMenu(Gamepad pad)
        {
            PlayerSlot slotToRemove = slots.Find(s => s.device == pad);
            if (slotToRemove == null)
                return;

            slots.Remove(slotToRemove);
            CompactSlots();
            OnSlotChanged?.Invoke(slotToRemove, false);
        }

        /// <summary>
        /// Ensures slot indices remain continuous after removal.
        /// </summary>
        void CompactSlots()
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i].index = i;
        }
        #endregion


        #region Slot Management (In-Game)
        /// <summary>
        /// Reconnects or assigns pad to a player slot during gameplay.
        /// </summary>
        void AssignInGame(Gamepad pad)
        {
            var existing = slots.Find(s => s.device == pad);
            if (existing != null)
            {
                existing.active = true;
                OnSlotChanged?.Invoke(existing, true);
                return;
            }

            var empty = slots.Find(s => !s.active);
            if (empty != null)
            {
                empty.device = pad;
                empty.active = true;
                OnSlotChanged?.Invoke(empty, true);
            }
        }

        /// <summary>
        /// Marks a slot inactive when its pad disconnects.
        /// </summary>
        void DeactivateInGame(Gamepad pad)
        {
            var slot = slots.Find(s => s.device == pad);
            if (slot != null)
            {
                slot.active = false;
                OnSlotChanged?.Invoke(slot, false);
            }
        }
        #endregion
    }
}
