using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Lightweight trigger component used to detect tagged objects
    /// and relay them as typed <see cref="DetectionType"/> events.
    ///
    /// Configurable for both "enter" and "exit" detections.
    /// Used by <see cref="DetectionController"/>.
    /// </summary>
    public class Detector : MonoBehaviour
    {
        UnityAction<Collider, DetectionType> EnterAction;
        UnityAction<Collider, DetectionType> ExitAction;

        List<string> enterTags = new();
        List<string> exitTags = new();
        bool enterTriggering;
        bool exitTriggering;

        /// <summary>Registers callback for OnTriggerEnter events.</summary>
        public void InitEnter(UnityAction<Collider, DetectionType> EnterAction)
        {
            enterTriggering = true;
            this.EnterAction = EnterAction;
        }

        /// <summary>Registers callback for OnTriggerExit events.</summary>
        public void InitExit(UnityAction<Collider, DetectionType> ExitAction)
        {
            exitTriggering = true;
            this.ExitAction = ExitAction;
        }

        /// <summary>Adds a detection type to listen for (enter or exit).</summary>
        public void AddDetection(DetectionType detectionType, bool enter)
        {
            string type = detectionType.ToString();
            if (enter)
            {
                if (!enterTags.Contains(type))
                    enterTags.Add(type);
            }
            else
            {
                if (!exitTags.Contains(type))
                    exitTags.Add(type);
            }
        }

        /// <summary>Removes a detection type from active list.</summary>
        public void RemoveDetection(DetectionType detectionType, bool enter)
        {
            string type = detectionType.ToString();
            if (enter)
            {
                if (enterTags.Contains(type))
                    enterTags.Remove(type);
            }
            else
            {
                if (exitTags.Contains(type))
                    exitTags.Remove(type);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!enterTriggering)
                return;

            for (int i = 0; i < enterTags.Count; i++)
            {
                if (other.CompareTag(enterTags[i]))
                {
                    if (Enum.TryParse(enterTags[i], out DetectionType result))
                        EnterAction?.Invoke(other, result);
                    break;
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!exitTriggering)
                return;

            for (int i = 0; i < exitTags.Count; i++)
            {
                if (Enum.TryParse(exitTags[i], out DetectionType result))
                    ExitAction?.Invoke(other, result);
            }
        }
    }
}
