using System.Collections.Generic;
using ClumsyCrew.Characters;
using ClumsyCrew.Core;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Minigames
{
    /// <summary>
    /// Manages the behavior, animations, and reactions of the background crowd during minigames.
    /// Handles cheering, laughing, throwing items, and reacting to player actions dynamically.
    /// </summary>
    public class CrowdController : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip crowdStartChanting;
        [SerializeField] private AudioClip crowdLoudOnStart;
        [SerializeField] private AudioClip crowdBoo;
        [SerializeField] private AudioClip crowdWinner;
        [SerializeField] private AudioClip[] crowdLaughing;
        [SerializeField] private AudioClip[] crowdClapping;

        [Header("Animations")]
        [SerializeField] private AnimValueDef cheerAnim;
        [SerializeField] private AnimValueDef chantAnim;
        [SerializeField] private AnimValueDef cheerStrongAnim;
        [SerializeField] private AnimValueDef throwAnim;
        [SerializeField] private AnimValueDef laughAnim;
        [SerializeField] private AnimValueDef applaudAnim;
        [SerializeField] private AnimValueDef pickWeaponAnim;

        [Header("Crowd Props")]
        [SerializeField] private GiftObject giftForCrowd;
        [SerializeField] private List<Transform> weaponPickingSpots;
        [SerializeField] private List<WeaponDefinition> weaponsForCrowd;
        [SerializeField] private List<ReactionType> reactionToLaughAt;

        [Header("Behavior Tuning")]
        [SerializeField] private float minimumTargetMovement = 3f;
        [SerializeField] private Vector2 agentCheckInterval = new(2f, 3f);
        [SerializeField] private Vector2 lookAtUpdateInterval = new(0.5f, 1f);
        [SerializeField] private Vector2 fencePosition = new(5.2f, 7.5f);
        [SerializeField] private Vector2 fencePositionDown = new(-6f, -8f);
        [SerializeField] private float npcXOffset = 4f;
        [SerializeField] private Vector2 triggerWeaponOrGift = new(0f, 30f);
        [SerializeField] private float spawnGiftAfterAnim = 0.22f;
        [SerializeField] private float radiusForGiftThrow = 3f;
        [SerializeField] private float chanceToLaughAt = 0.8f;
        [SerializeField] private float chanceToApplaud = 0.8f;

        private readonly List<NPCTargetValues> npcValues = new();
        private NPCTargetValues cachedValue;
        private float weaponOrGiftTimer;
        private float applaudCooldown;
        private bool gameActive;

        #region Unity Lifecycle
        private void Start()
        {
            CharactersManager.Instance.OnNPCSpawned += OnNPCSpawned;
            CharactersManager.Instance.OnMainCharacterSpawned += OnMainCharacterSpawned;
            MinigameManager.Instance.Damage.ItemDestroyed += OnItemDestroyed;
            MinigameManager.Instance.OnMinigameEnd += OnMinigameEnd;
            GameManager.Instance.GameStateChanged += OnGameStateChanged;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (applaudCooldown > 0)
                applaudCooldown -= deltaTime;

            if (gameActive)
            {
                weaponOrGiftTimer -= deltaTime;
                if (weaponOrGiftTimer < 0)
                {
                    weaponOrGiftTimer = Random.Range(triggerWeaponOrGift.x, triggerWeaponOrGift.y);
                    TrySpawnWeaponOrGift();
                }
            }

            UpdateCrowdBehavior(deltaTime);
        }

        private void OnDestroy()
        {
            CharactersManager.Instance.OnNPCSpawned -= OnNPCSpawned;
            CharactersManager.Instance.OnMainCharacterSpawned -= OnMainCharacterSpawned;
            MinigameManager.Instance.Damage.ItemDestroyed -= OnItemDestroyed;
            MinigameManager.Instance.OnMinigameEnd -= OnMinigameEnd;
            GameManager.Instance.GameStateChanged -= OnGameStateChanged;

            LeanTween.cancel(gameObject);
        }
        #endregion

        #region Event Handlers
        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.ExitingMinigame)
                SoundManager.Instance.Sources.FadeInOutSource(AudioSourceType.Crowd, false);
            else if (state == GameState.Minigame)
            {
                gameActive = true;
                weaponOrGiftTimer = Random.Range(triggerWeaponOrGift.x, triggerWeaponOrGift.y);
            }
        }

        private void OnMainCharacterSpawned(MainCharacterScript main)
        {
            main.Reactions.OnReactionChanged += OnReactionChanged;
            main.ToolUseController.OnChargeHit += OnChargeHit;
        }

        private void OnNPCSpawned(NPCScript npc)
        {
            if (npc.NPCRole != NPCRole.Spectator) return;

            var targets = CharactersManager.Instance.MainCharacters;
            MainCharacterScript target = targets.GetRandom();

            npcValues.Add(new NPCTargetValues
            {
                script = npc,
                target = target,
                lastTargetX = target.transform.position.x,
                checkingTargetTimer = Random.Range(agentCheckInterval.x, agentCheckInterval.y),
                lookAtTargetTimer = Random.Range(lookAtUpdateInterval.x, lookAtUpdateInterval.y),
                targetRotation = target.transform.rotation,
                upperRow = npc.transform.position.z > 0
            });

            npc.PlayBoolAnim(cheerAnim, true, Random.Range(0f, 1.5f));
        }

        private void OnReactionChanged(ReactionDef def, int playerIndex)
        {
            if (reactionToLaughAt.Contains(def.Type))
                LaughAtPlayer(playerIndex);
        }

        private void OnChargeHit(ToolUseType type, int playerIndex)
        {
            if (type == ToolUseType.Bad)
                LaughAtPlayer(playerIndex);
        }

        private void OnItemDestroyed(JobItem item, int playerIndex)
        {
            TryToApplaud(playerIndex);
        }

        private void OnMinigameEnd(int playerIndex)
        {
            gameActive = false;
            SoundManager.Instance.PlaySound(crowdWinner);

            Transform winner = CharactersManager.Instance.AllCharacters[playerIndex].transform;
            foreach (var npc in npcValues)
            {
                Vector3 posToGo = new(winner.position.x + Random.Range(-2, 2), winner.position.y, winner.position.z + Random.Range(-2, 2));
                npc.script.MoveTo(posToGo, default, MoveSpeed.Sprint);
            }
        }
        #endregion

        #region Crowd Reactions
        public void CrowdOnStart(float delayChanting, float delayStart)
        {
            LeanTween.delayedCall(gameObject, delayChanting, () =>
            {
                SoundManager.Instance.PlaySound(crowdStartChanting);
                foreach (var npc in npcValues)
                    npc.script.PlayTriggerAnim(chantAnim);
            });

            LeanTween.delayedCall(gameObject, delayStart, () =>
            {
                SoundManager.Instance.PlaySound(crowdLoudOnStart);
                foreach (var npc in npcValues)
                    npc.script.PlayTriggerAnim(cheerStrongAnim);
            });
        }

        private void LaughAtPlayer(int playerIndex)
        {
            SoundManager.Instance.PlaySound(crowdLaughing.GetRandom());
            foreach (var npc in npcValues)
            {
                if (npc.target.Index == playerIndex && Random.value < chanceToLaughAt)
                    npc.script.PlayTriggerAnim(laughAnim);
            }
        }

        private void TryToApplaud(int playerIndex)
        {
            if (applaudCooldown > 0) return;

            SoundManager.Instance.PlaySound(crowdClapping.GetRandom());
            foreach (var npc in npcValues)
            {
                if (npc.target.Index == playerIndex && Random.value < chanceToApplaud)
                    npc.script.PlayTriggerAnim(applaudAnim);
            }

            applaudCooldown = 4f;
        }
        #endregion

        #region NPC Control
        private void UpdateCrowdBehavior(float deltaTime)
        {
            for (int i = 0; i < npcValues.Count; i++)
            {
                cachedValue = npcValues[i];

                if (!cachedValue.preventRotUpdate)
                    cachedValue.script.transform.rotation = Quaternion.Lerp(
                        cachedValue.script.transform.rotation,
                        cachedValue.targetRotation,
                        deltaTime * 3f);

                cachedValue.lookAtTargetTimer -= deltaTime;
                cachedValue.checkingTargetTimer -= deltaTime;

                if (cachedValue.checkingTargetTimer <= 0)
                {
                    cachedValue.checkingTargetTimer = Random.Range(agentCheckInterval.x, agentCheckInterval.y);
                    CheckTargetPos(cachedValue);
                }

                if (cachedValue.lookAtTargetTimer <= 0)
                {
                    cachedValue.lookAtTargetTimer = Random.Range(lookAtUpdateInterval.x, lookAtUpdateInterval.y);
                    UpdateLookAt(cachedValue);
                }
            }
        }

        private void CheckTargetPos(NPCTargetValues value)
        {
            if (Mathf.Abs(value.lastTargetX - value.target.transform.position.x) < minimumTargetMovement)
                return;

            value.preventRotUpdate = true;
            value.lastTargetX = value.target.transform.position.x;

            Vector3 posToMove = new(
                value.lastTargetX + Random.Range(-npcXOffset, npcXOffset),
                0,
                Random.Range(value.upperRow ? fencePosition.x : fencePositionDown.x,
                             value.upperRow ? fencePosition.y : fencePositionDown.y));

            value.script.MoveTo(posToMove, _ => value.preventRotUpdate = false);
        }

        private void UpdateLookAt(NPCTargetValues value)
        {
            Vector3 dir = value.target.transform.position - value.script.transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                value.targetRotation = Quaternion.LookRotation(dir);
        }
        #endregion

        #region Weapons & Gifts
        private void TrySpawnWeaponOrGift()
        {
            if (npcValues.Count == 0) return;

            NPCTargetValues npc = npcValues.GetRandom();
            bool gift = npc.target.Levels[2] / 100f > Random.value;

            if (!gift)
                SpawnWeaponThrow(npc);
            else
                SpawnGiftThrow(npc);
        }

        private void SpawnWeaponThrow(NPCTargetValues npc)
        {
            Vector3 originalPos = npc.script.transform.position;
            npc.script.AssignAgentImportance(true);

            npc.script.MoveTo(FindClosestWeaponSpot(npc.script.transform).position, _ =>
            {
                npc.script.AssignAgentImportance(false);
                npc.script.PlayTriggerAnim(pickWeaponAnim);

                LeanTween.delayedCall(gameObject, 0.5f, () =>
                {
                    npc.script.ThrowWeapon(weaponsForCrowd.GetRandom(), npc.target.transform.position);

                    LeanTween.delayedCall(gameObject, 1f, () =>
                    {
                        npc.script.MoveTo(originalPos);
                    });
                });
            }, MoveSpeed.Run, 0.8f);
        }

        private void SpawnGiftThrow(NPCTargetValues npc)
        {
            npc.script.PlayTriggerAnim(throwAnim);
            LeanTween.delayedCall(spawnGiftAfterAnim, () =>
            {
                Vector3 randomOffset = new(
                    Random.Range(-radiusForGiftThrow, radiusForGiftThrow),
                    0,
                    Random.Range(-radiusForGiftThrow, radiusForGiftThrow));

                npc.script.ThrowGift(giftForCrowd, npc.target.transform.position + randomOffset);
            });
        }

        private Transform FindClosestWeaponSpot(Transform from)
        {
            int closestIndex = 0;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < weaponPickingSpots.Count; i++)
            {
                float distance = Vector3.Distance(from.position, weaponPickingSpots[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return weaponPickingSpots[closestIndex];
        }
        #endregion
    }
}
