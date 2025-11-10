#if UNITY_EDITOR
using ClumsyCrew.Characters;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace ClumsyCrew.Editor
{
    /// <summary>
    /// Custom scene editor for <see cref="CharacterRigAnchors"/>.
    /// Draws 3D position and rotation handles for anchor transforms.
    /// Integrates with Unity's Undo system.
    /// </summary>
    [CustomEditor(typeof(CharacterRigAnchors))]
    public class CharacterRigAnchorsEditor : OdinEditor
    {
        void OnSceneGUI()
        {
            CharacterRigAnchors anchors = (CharacterRigAnchors)target;
            if (!anchors.ShowHandles) return;

            foreach (var setup in anchors.AnchorSetups)
            {
                if (setup.anchorTransform == null)
                    continue;

                // --- Position Handle ---
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(setup.anchorTransform.position, setup.anchorTransform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(setup.anchorTransform, "Move Anchor");
                    setup.anchorTransform.position = newPos;
                    setup.localPos = setup.anchorTransform.localPosition;
                }

                // --- Rotation Handle ---
                EditorGUI.BeginChangeCheck();
                Quaternion newRot = Handles.RotationHandle(setup.anchorTransform.rotation, setup.anchorTransform.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(setup.anchorTransform, "Rotate Anchor");
                    setup.anchorTransform.rotation = newRot;
                    setup.localRot = setup.anchorTransform.localRotation.eulerAngles;
                }
            }
        }
    }
}
#endif

