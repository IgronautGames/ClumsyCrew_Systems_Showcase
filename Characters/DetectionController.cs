using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Central controller that manages which detection types
    /// (e.g. Pickable, Edge, JobPlace, etc.) the attached Detector listens for.
    ///
    /// Acts as a bridge between a <see cref="CharacterScript"/> and its <see cref="Detector"/>.
    /// Subscribes to character events to dynamically enable / disable detections.
    /// </summary>
    public class DetectionController : MonoBehaviour
    {
        /// <summary>Raised when a detection event occurs (OnTriggerEnter).</summary>
        public event UnityAction<DetectionType, Collider> DetectionDetected;
        /// <summary>Raised when a detection target is exited (OnTriggerExit).</summary>
        public event UnityAction<DetectionType, Collider> DetectionExited;

        [SerializeField] Detector detector;

        List<DetectionType> detectionsOnEnter;
        List<DetectionType> detectionsOnExit;
        CharacterScript character;

        void Start()
        {
            if (character is MainCharacterScript main)
                main.InteractionController.OnPickingEnabled += OnPickingEnabled;

            detector.InitEnter(DetectionEnter);
            detector.InitExit(DetectionExit);

            // Register initial detections
            for (int i = 0; i < detectionsOnEnter.Count; i++)
                detector.AddDetection(detectionsOnEnter[i], true);

            for (int i = 0; i < detectionsOnExit.Count; i++)
                detector.AddDetection(detectionsOnExit[i], false);
        }

        /// <summary>
        /// Initializes the controller with detection lists and owning character.
        /// </summary>
        public void Init(List<DetectionType> detectionsOnEnter, List<DetectionType> detectionsOnExit, CharacterScript character)
        {
            this.detectionsOnEnter = detectionsOnEnter;
            this.detectionsOnExit = detectionsOnExit;
            this.character = character;
        }

        void OnPickingEnabled(bool enabled)
        {
            if (enabled)
            {
                detector.AddDetection(DetectionType.Pickable, true);
                detector.AddDetection(DetectionType.JobPlace, true);
            }
            else
            {
                detector.RemoveDetection(DetectionType.Pickable, true);
                detector.RemoveDetection(DetectionType.JobPlace, true);
            }
        }

        void DetectionEnter(Collider col, DetectionType type)
        {
            DetectionDetected?.Invoke(type, col);
        }

        void DetectionExit(Collider col, DetectionType type)
        {
            DetectionExited?.Invoke(type, col);
        }
    }
}
