using UnityEngine;
using UnityEngine.Events;

namespace ClumsyCrew.Core
{
    /// <summary>
    /// Handles playback of spatialized or flat (2D) audio in the world.
    /// Integrates with player positions for realistic attenuation and
    /// returns itself to a pool after playback.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SpatialAudioScript : MonoBehaviour
    {
        [SerializeField] private AudioSource source;

        /// <summary>
        /// Initializes and plays an audio clip with optional spatial blending.
        /// </summary>
        /// <param name="clip">The clip to play.</param>
        /// <param name="ReturnAction">Callback invoked when finished (for pooling).</param>
        /// <param name="real">If true, plays fully 3D. If false, uses pseudo-distance volume in 2D mode.</param>
        public void Init(AudioClip clip, UnityAction<SpatialAudioScript> ReturnAction, bool real)
        {
            if (clip == null)
            {
                Debug.LogWarning($"{nameof(SpatialAudioScript)}: Tried to play a null AudioClip.");
                ReturnAction?.Invoke(this);
                return;
            }

            source.clip = clip;
            source.spatialBlend = real ? 1f : 0f;
            source.volume = real ? 1f : CalculateInitialVolume();
            source.Play();

            // Return this object to the pool when the clip finishes
            LeanTween.delayedCall(clip.length, () => ReturnAction?.Invoke(this));
        }

        /// <summary>
        /// Estimates an initial volume for 2D sounds based on distance
        /// to the nearest active player, creating a sense of space even for flat audio.
        /// </summary>
        private float CalculateInitialVolume()
        {
            float closestDistance = float.MaxValue;

            foreach (var slot in GameManager.Instance.Players.GetActiveSlots())
            {
                if (slot.character == null) continue;

                float dist = Vector3.Distance(transform.position, slot.character.transform.position);
                if (dist < closestDistance)
                    closestDistance = dist;
            }

            const float maxDistance = 40f;
            return Mathf.Clamp01(1f - closestDistance / maxDistance);
        }
    }
}
