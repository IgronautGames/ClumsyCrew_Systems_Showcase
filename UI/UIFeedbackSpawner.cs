using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Spawns and animates floating UI elements (coins, hearts, etc.)
    /// using a simple object pool for performance.
    /// </summary>
    public class UIFeedbackSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] CollectedItemUI collectedItemPrefab;
        [SerializeField] Transform spawnParent;

        readonly List<CollectedItemUI> pool = new();

        /// <summary>
        /// Spawns a collectible UI element that flies toward a target position.
        /// </summary>
        public void SpawnCollectedItem(
            Sprite sprite,
            Vector2 startPos,
            Vector2 targetPos,
            UnityAction<int> onComplete,
            int amount = 0,
            float duration = 1f,
            float curveBack = 600f,
            float curveUp = 50f,
            Vector2 size = default)
        {
            CollectedItemUI item = GetFromPool();
            item.Init(
                sprite,
                startPos,
                targetPos,
                amount,
                onComplete,
                ReturnToPool,
                duration,
                curveUp,
                curveBack,
                size == default ? new Vector2(150, 150) : size
            );
        }

        /// <summary>
        /// Spawns a floating heart animation from the given position.
        /// </summary>
        public void SpawnFlyingHeart(Vector2 startPos, Vector2 size = default)
        {
            CollectedItemUI item = GetFromPool();
            item.FlyingHearth(
                IconsConfig.Instance.hearth,
                startPos,
                size == default ? new Vector2(120, 120) : size,
                ReturnToPool
            );
        }

        CollectedItemUI GetFromPool()
        {
            CollectedItemUI item;
            if (pool.Count > 0)
            {
                item = pool[0];
                pool.RemoveAt(0);
                item.gameObject.SetActive(true);
            }
            else
            {
                item = Instantiate(collectedItemPrefab, spawnParent);
            }

            return item;
        }

        void ReturnToPool(CollectedItemUI item)
        {
            item.gameObject.SetActive(false);
            pool.Add(item);
        }
    }
}
