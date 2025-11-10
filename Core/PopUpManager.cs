using ClumsyCrew.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Central UI manager responsible for handling popup windows.
    /// - Uses generic type-safe methods to open/close specific popup types.
    /// - Maintains a pool to avoid re-instantiating popups.
    /// - Broadcasts popup open/close events to other systems (audio, input locks, etc.).
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        /// <summary>
        /// Invoked whenever a popup is opened (bool = true) or closed (bool = false).
        /// </summary>
        public event UnityAction<PopUp, bool> OnPopUpOpened;

        [SerializeField] RectTransform content;

        readonly List<PopUp> popUpsPool = new();
        int numbOfOpenedPopups;

        public int NumbOfOpenedPopups => numbOfOpenedPopups;
        public RectTransform Content => content;

        #region Public API

        /// <summary>
        /// Returns an instance of popup of type <typeparamref name="T"/>.
        /// If already in pool, reuses it; otherwise instantiates from PrefabsConfig.
        /// </summary>
        public T GetPopUp<T>() where T : PopUp
        {
            // Try reuse existing instance
            T existing = popUpsPool.OfType<T>().FirstOrDefault();
            if (existing != null)
            {
                existing.Init(content);
                return existing;
            }

            // Fallback: instantiate new prefab
            T prefab = PrefabsConfig.Instance.PopUps.OfType<T>().FirstOrDefault();
            if (prefab == null)
            {
                Debug.LogError($"No prefab of type {typeof(T).Name} found in PrefabsConfig!");
                return null;
            }

            T newPopUp = Instantiate(prefab, content);
            popUpsPool.Add(newPopUp);
            return newPopUp;
        }

        /// <summary>
        /// Opens popup of given type.
        /// </summary>
        public void OpenPopUp<T>() where T : PopUp
        {
            GetPopUp<T>().OpenClose(true);
        }

        /// <summary>
        /// Closes popup of given type (if it exists and is active).
        /// </summary>
        public void ClosePopUp<T>() where T : PopUp
        {
            T existing = popUpsPool.OfType<T>().FirstOrDefault();
            if (existing != null)
                existing.OpenClose(false);
        }

        /// <summary>
        /// Called by PopUp instance when opening begins.
        /// Handles mutual exclusivity and event broadcasting.
        /// </summary>
        public void OpeningPopUp(PopUp popup)
        {
            OnPopUpOpened?.Invoke(popup, true);

            if (numbOfOpenedPopups > 0 && popup.CloseOtherPopUpsOnOpeningThis)
            {
                foreach (var pop in popUpsPool)
                {
                    if (pop != popup && pop.IsOpen && !pop.IgnoreClosingOnAnotherPopUpOpen)
                        pop.OpenClose(false);
                }
            }

            numbOfOpenedPopups++;
        }

        /// <summary>
        /// Called by PopUp instance when closing.
        /// Decreases active counter and triggers event.
        /// </summary>
        public void ClosePopUp(PopUp popup)
        {
            numbOfOpenedPopups--;
            OnPopUpOpened?.Invoke(popup, false);
        }

        /// <summary>
        /// Removes a popup instance from pool (not yet used).
        /// </summary>
        public void DestroyPopUp<T>() where T : PopUp
        {
            // Optional future feature:
            // remove popup instance from pool and destroy object.
        }

        #endregion
    }
}
