using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using static ClumsyCrew.GameEnums;

namespace ClumsyCrew.Characters
{
    /// <summary>
    /// Stores and regenerates transform anchors attached to character rig bones.
    /// Used to attach items such as tools, carried props, or customization pieces.
    /// Provides both editor-time anchor placement and runtime lookup by <see cref="AnchorType"/>.
    /// </summary>
    [ExecuteAlways]
    public class CharacterRigAnchors : MonoBehaviour, ICharacterWithCarrying
    {
        [SerializeField] Transform rigRoot;
        [SerializeField] List<AnchorSetup> anchorSetups = new();
        [SerializeField] bool showHandles = true;

        Dictionary<AnchorType, Transform> anchorsDict = new();
        public IReadOnlyDictionary<AnchorType, Transform> Anchors => anchorsDict;
        public List<AnchorSetup> AnchorSetups => anchorSetups;
        public bool ShowHandles => showHandles;

        #region Editor Utilities

        [Button]
        public void PopulateDefaults()
        {
            anchorSetups.Clear();

            anchorSetups.Add(new AnchorSetup { type = AnchorType.Hands, boneName = "Hand_R", localPos = new(0.074f, 0.286f, 0.222f), localRot = new(10.855f, 81.878f, -5.211f) });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.Back, boneName = "Spine_03", localPos = new(0.6467f, 0.309f, 0.0628f), localRot = new(76.369f, -107.453f, -14.557f) });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.LShoulder, boneName = "Hand_L", localPos = new(0.246f, -0.343f, 0.484f), localRot = new(-41.46f, 87.39f, -59.981f) });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.RShoulder, boneName = "Hand_R", localPos = new(-0.22f, 0.459f, -0.492f), localRot = new(135.913f, -6.922f, -46.518f) });

            // Basic anchors
            anchorSetups.Add(new AnchorSetup { type = AnchorType.Hips, boneName = "Hips" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.LHand, boneName = "Hand_L" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.RHand, boneName = "Hand_R" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.LLeg, boneName = "Ankle_L" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.RLeg, boneName = "Ankle_R" });

            // Cosmetic anchors
            anchorSetups.Add(new AnchorSetup { type = AnchorType.CustBeard, boneName = "Head" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.CustHat, boneName = "Head" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.CustGlasses, boneName = "Head" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.CustHair, boneName = "Head" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.CustFaceMask, boneName = "Head" });

            // Generic
            anchorSetups.Add(new AnchorSetup { type = AnchorType.Head, boneName = "Head" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.ToolHandL, boneName = "Hand_L" });
            anchorSetups.Add(new AnchorSetup { type = AnchorType.ToolHandR, boneName = "Hand_R" });

            RegenerateAnchors();
        }

        [Button]
        public void RegenerateAnchors()
        {
            anchorsDict.Clear();

            foreach (var setup in anchorSetups)
            {
                if (rigRoot == null || string.IsNullOrEmpty(setup.boneName))
                    continue;

                Transform bone = FindChildByName(rigRoot, setup.boneName);
                if (bone == null)
                {
                    Debug.LogWarning($"Bone '{setup.boneName}' not found for {setup.type}");
                    continue;
                }

#if UNITY_EDITOR
                if (setup.anchorTransform != null)
                    DestroyImmediate(setup.anchorTransform.gameObject);
#endif
                GameObject anchorGO = new($"{setup.type}Anchor");
                anchorGO.transform.SetParent(bone, false);
                anchorGO.transform.localPosition = setup.localPos;
                anchorGO.transform.localRotation = Quaternion.Euler(setup.localRot);

                setup.anchorTransform = anchorGO.transform;
                anchorsDict[setup.type] = anchorGO.transform;
            }
        }

        [Button]
        public void SaveAnchorsFromScene()
        {
            foreach (var setup in anchorSetups)
            {
                if (setup.anchorTransform == null) continue;
                setup.localPos = setup.anchorTransform.localPosition;
                setup.localRot = setup.anchorTransform.localRotation.eulerAngles;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        #endregion

        #region Runtime Initialization

        void Awake()
        {
            if (!Application.isPlaying)
                return;

            anchorsDict.Clear();
            foreach (var setup in anchorSetups)
                if (setup.anchorTransform != null && !anchorsDict.ContainsKey(setup.type))
                    anchorsDict.Add(setup.type, setup.anchorTransform);
        }

        #endregion

        #region Helpers

        Transform FindChildByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindChildByName(child, name);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// Returns the transform where a job item should be attached based on its type and order.
        /// </summary>
        public Transform GetCarringPos(JobItemType jobItemType, int itemNumb = 1)
        {
            if (jobItemType == JobItemType.Stone)
                return anchorsDict[AnchorType.Back];
            else if (jobItemType == JobItemType.TrashBag)
                return itemNumb == 2 ? anchorsDict[AnchorType.RShoulder] : anchorsDict[AnchorType.LShoulder];
            else
                return anchorsDict[AnchorType.Hands];
        }

        void OnDrawGizmos()
        {
            if (!showHandles) return;

            Gizmos.color = Color.red;
            foreach (var setup in anchorSetups)
            {
                if (setup.anchorTransform != null)
                    Gizmos.DrawSphere(setup.anchorTransform.position, 0.03f);
            }
        }

        #endregion
    }
}
