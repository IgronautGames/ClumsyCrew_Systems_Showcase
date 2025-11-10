using System.Collections.Generic;
using ClumsyCrew.Characters;
using UnityEngine;
using UnityEngine.Events;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Minigames
{
    /// <summary>
    /// Handles activation, timing, and hit detection for tool colliders ("hitting detectors").
    /// Each detector corresponds to a specific <see cref="HitterType"/> and notifies when a hit occurs.
    /// </summary>
    public class HittingController : MonoBehaviour
    {
        [Header("Default Detectors")]
        [SerializeField] List<HittingDetector> defaultDetectors = new();

        private CharacterScript character;

        /// <summary>
        /// Invoked when any active hitting detector registers a hit on a <see cref="Destroyable"/>.
        /// </summary>
        public event UnityAction<Destroyable> OnHitByTool;

        private readonly Dictionary<HitterType, float> timers = new();
        private readonly Dictionary<HitterType, HittingDetector> detectors = new();

        private void Start()
        {
            if (character == null)
                Debug.LogWarning("HittingController started without being initialized.");

            character.ToolSpawner.OnHittingDetectorAdd += HandleDetectorAdd;

            foreach (var det in defaultDetectors)
                HandleDetectorAdd(det, true);
        }

        private void Update()
        {
            foreach (var hitter in detectors.Keys)
            {
                if (timers[hitter] > -9f)
                {
                    timers[hitter] -= Time.deltaTime;
                    if (timers[hitter] < 0f)
                    {
                        detectors[hitter].gameObject.SetActive(false);
                        timers[hitter] = -10f;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the controller for a specific character.
        /// </summary>
        public void Init(CharacterScript character)
        {
            this.character = character;
        }

        private void HandleDetectorAdd(HittingDetector detector, bool add)
        {
            if (add)
            {
                if (!detectors.ContainsKey(detector.Type))
                {
                    detector.OnHit += HandleHit;
                    detectors.Add(detector.Type, detector);
                    timers.Add(detector.Type, -1f);
                }
            }
            else
            {
                if (detectors.ContainsKey(detector.Type))
                {
                    detector.OnHit -= HandleHit;
                    detectors.Remove(detector.Type);
                    timers.Remove(detector.Type);
                }
            }
        }

        /// <summary>
        /// Temporarily enables a detector for a given time window.
        /// </summary>
        public void ActivateDetector(HitterType hitter, float activeTime)
        {
            if (!detectors.ContainsKey(hitter))
                return;

            detectors[hitter].gameObject.SetActive(true);
            timers[hitter] = activeTime;
        }

        /// <summary>
        /// Immediately deactivates a specific hitting detector.
        /// </summary>
        public void DeactivateDetector(HitterType hitter)
        {
            if (detectors.ContainsKey(hitter))
                detectors[hitter].gameObject.SetActive(false);
        }

        private void HandleHit(Destroyable target)
        {
            OnHitByTool?.Invoke(target);
        }
    }
}
