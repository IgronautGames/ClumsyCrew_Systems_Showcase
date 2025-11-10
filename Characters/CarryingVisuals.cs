using ClumsyCrew.Minigames;
using System.Collections.Generic;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Handles the visual representation and motion simulation of items carried by the player.
    /// Items bounce, tilt, and shift naturally during running, creating a fun cartoon feel.
    /// </summary>
    public class CarryingVisuals : MonoBehaviour
    {
        [Header("Jumping Rows Tuning")]
        [Tooltip("Speed of rising motion during jump.")]
        public float riseSpeed = 4f;
        [Tooltip("Maximum height an item row can reach when jumping.")]
        public float maxJumpHeight = 0.5f;
        [Tooltip("Maximum sideways (X) offset during motion.")]
        public float maxXShift = 0.2f;
        [Tooltip("Maximum sideways (Z) offset during motion.")]
        public float maxZShift = 0.2f;
        [Tooltip("Maximum random rotation angle applied at peak.")]
        public float maxRandomRotation = 15f;
        [Tooltip("How strongly gravity pulls rows back down.")]
        public float gravityAcceleration = 52f;

        float itemsJumpMultiplier = 1f;
        int itemsPicked = 0;

        protected JobItemType carryingType;
        protected Vector3 carryingItemScale;
        protected Transform carryingParent;

        int amountPerRow;
        float rowOffset;
        Vector3 newRowPosition;

        readonly List<GameObject> armsRows = new();
        readonly List<CarryingRow> activeRows = new();

        float carryingDifficultyVelocity;
        float smoothedCarryingDifficulty;
        float carryingDifficultyTarget;

        protected MainCharacterScript mainChar;
        protected IReadOnlyDictionary<AnchorType, Transform> anchors;

        public float SmoothedCarryingDifficulty => smoothedCarryingDifficulty;

        private void Start()
        {
            anchors = mainChar.CharacterRigAnchors.Anchors;

            mainChar.Animator.OnRunPeak += RunPeak;
            mainChar.CarryingLogic.OnCarrying += OnCarrying;
            mainChar.CarryingLogic.CarryingTypeChanged += OnCarryingTypeChanged;
        }

        private void Update()
        {
            smoothedCarryingDifficulty = Mathf.SmoothDamp(
                smoothedCarryingDifficulty,
                carryingDifficultyTarget,
                ref carryingDifficultyVelocity,
                0.3f);

            mainChar.Animator.ParameterFloat("CarryingDifficulty", smoothedCarryingDifficulty);

            if (itemsPicked > 0)
                SimulateItemMovement(Time.deltaTime);
        }

        public void Init(MainCharacterScript mainChar)
        {
            this.mainChar = mainChar;
        }

        #region Carry Management

        public void MoveOnArms(Pickable pickable)
        {
            pickable.Picked(true);
            itemsPicked++;
            bool newRow = AddToRow(pickable);

            Vector3 endPos = new(0, 0, ((itemsPicked - 1) % amountPerRow - 2) * carryingItemScale.z);
            Vector3 originalPos = newRow ? newRowPosition : Vector3.zero;

            // Nice cartoony hop
            Vector3 topPos = (pickable.transform.localPosition + pickable.transform.parent.localPosition) / 2;
            topPos.y = endPos.y + 2f;

            LeanTween.moveLocal(pickable.gameObject, topPos, 0.25f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    LeanTween.moveLocal(pickable.gameObject, endPos, 0.15f)
                        .setEase(LeanTweenType.easeInQuad)
                        .setOnComplete(() =>
                        {
                            if (newRow)
                                activeRows.Add(new() { transform = pickable.transform.parent, originalLocalPosition = originalPos });
                        });
                });

            LeanTween.rotateLocal(pickable.gameObject, Vector3.zero, 0.4f).setEase(LeanTweenType.easeInOutQuad);
            mainChar.VFXController.RefreshHiglights();
        }

        bool AddToRow(Pickable pickable)
        {
            bool newRow = false;

            if (carryingType == JobItemType.TrashBag)
            {
                pickable.transform.parent = itemsPicked == 2
                    ? anchors[AnchorType.RShoulder]
                    : anchors[AnchorType.LShoulder];
            }
            else
            {
                if (amountPerRow == 1 || itemsPicked % amountPerRow == 1)
                {
                    Vector3 rowLocalPos = new(0, ((itemsPicked - 1) / amountPerRow) * carryingItemScale.y + rowOffset);
                    GameObject go = new("Row");
                    go.transform.parent = carryingParent;
                    go.transform.SetLocalPositionAndRotation(rowLocalPos, Quaternion.identity);

                    armsRows.Add(go);
                    newRowPosition = rowLocalPos;
                    newRow = true;
                }

                pickable.transform.parent = armsRows[^1].transform;
                pickable.RowNumb = armsRows.Count - 1;
            }

            return newRow;
        }

        public void RemoveItemFromCarry(Pickable item)
        {
            LeanTween.cancel(item.gameObject);
            item.transform.SetParent(MinigameManager.Instance.CurrentMinigame.transform);
            item.Picked(false);
            itemsPicked--;

            GameObject row = armsRows[item.RowNumb];
            if (row.transform.childCount == 0)
            {
                Destroy(row);
                armsRows.RemoveAt(item.RowNumb);

                if (activeRows.Count > item.RowNumb)
                    activeRows.RemoveAt(item.RowNumb);
            }

            mainChar.VFXController.RefreshHiglights();
        }

        public void RemoveAllPicked(List<Pickable> picked)
        {
            for (int i = picked.Count - 1; i >= 0; i--)
                RemoveItemFromCarry(picked[i]);
        }

        #endregion

        #region Trash Bag Handling

        public void StackTrashBag(JobItem bag, bool left)
        {
