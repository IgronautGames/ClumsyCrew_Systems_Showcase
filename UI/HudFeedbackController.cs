using ClumsyCrew.Characters;
using ClumsyCrew.Core;
using ClumsyCrew.Minigames;
using System.Collections.Generic;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.UI
{
    /// <summary>
    /// Controls all in-game HUD feedback messages for a player.
    /// 
    /// Subscribes to gameplay events (tool hits, falls, reactions, etc.)
    /// and spawns short visual messages such as “Perfect!”, “Double Smash!”, or “Lost Balance”.
    /// 
    /// Uses an internal pool to avoid unnecessary instantiations.
    /// </summary>
    public class HudFeedbackController : MonoBehaviour
    {
        MainCharacterScript mainChar;
        ToolDefinition currentToolDef;

        readonly Dictionary<GameHudFeedbackType, List<GameHudFeedback>> pool = new();

        void Start()
        {
            mainChar.ToolUseController.OnChargeHit += OnChargeHit;
            mainChar.ToolUseController.OnMultihit += OnMultihit;
            mainChar.ToolUseController.OnMultiDestroyed += OnMultiDestroyed;
            mainChar.ToolController.ToolActivated += OnToolActivated;
            mainChar.WeaponScript.OnWeaponTriggered += OnWeaponTriggered;
            mainChar.StabilityMeterController.OnFall += OnFall;
            mainChar.Reactions.OnReactionChanged += OnReactionChanged;
        }

        /// <summary>Assigns the character this HUD feedback system will follow.</summary>
        public void Init(MainCharacterScript main)
        {
            mainChar = main;
        }

        void OnToolActivated(ItemSubType type, bool activated)
        {
            if (activated)
                currentToolDef = DefinitionsManager.Instance.ToolDefsDict[type];
        }

        void OnChargeHit(ToolUseType type, int playerNumb)
        {
            SpawnFeedback(currentToolDef.ChargePercents[(int)type].feedbackTextType);
        }

        void OnMultihit(int multihit)
        {
            SpawnFeedback(GameHudFeedbackType.Multihit);
        }

        void OnMultiDestroyed(int multiDestroyed)
        {
            if (multiDestroyed == 2)
                SpawnFeedback(GameHudFeedbackType.DoubleSmash);
            else if (multiDestroyed == 3)
                SpawnFeedback(GameHudFeedbackType.TripleSmash);
            else if (multiDestroyed > 3)
                SpawnFeedback(GameHudFeedbackType.MultiSmash);
        }

        void OnWeaponTriggered(WeaponItem item, CharacterScript target)
        {
            if (item.Def.HasHudFeedback)
                SpawnFeedback(item.Def.HudFeedbackType);
        }

        void OnFall()
        {
            SpawnFeedback(GameHudFeedbackType.LostBalance);
        }

        void OnReactionChanged(ReactionDef def, int player)
        {
            if (def.HasHudFeedback)
                SpawnFeedback(def.HudFeedback);
        }

        /// <summary>
        /// Spawns a HUD feedback popup based on the given feedback type.
        /// </summary>
        public GameHudFeedback SpawnFeedback(GameHudFeedbackType type)
        {
            if (!pool.ContainsKey(type))
                pool.Add(type, new List<GameHudFeedback>());

            GameHudFeedback feedback;
            if (pool[type].Count == 0)
            {
                HudFeedbackDef def = DefinitionsManager.Instance.HudFeedbackDefsDict[type];
                feedback = Instantiate(def.prefab, transform);
                feedback.Init(def, ReturnToPool);
            }
            else
            {
                feedback = pool[type][0];
                pool[type].RemoveAt(0);
                feedback.gameObject.SetActive(true);
            }

            return feedback;
        }

        void ReturnToPool(GameHudFeedback feedback)
        {
            pool[feedback.Type].Add(feedback);
            feedback.gameObject.SetActive(false);
        }
    }
}
