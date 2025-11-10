using UnityEditor;
using UnityEngine;
using System.Linq;

namespace ClumsyCrew.Editor
{
    /// <summary>
    /// Custom Unity Editor tool for renaming bone paths across multiple animation clips.
    /// 
    /// Features:
    /// - Batch renames bone references in selected AnimationClips.
    /// - Allows folder-based selection via file dialog.
    /// - (Optional) Can remove scale property curves to prevent unwanted scaling.
    /// </summary>
    public class BoneRenamer : EditorWindow
    {
        string oldBoneName = "OldBoneName";
        string newBoneName = "NewBoneName";
        AnimationClip[] animationClips;

        [MenuItem("Tools/Bone Renamer")]
        public static void ShowWindow()
        {
            GetWindow<BoneRenamer>("Bone Renamer");
        }

        void OnGUI()
        {
            GUILayout.Label("Rename Bones in Animation Clips", EditorStyles.boldLabel);

            oldBoneName = EditorGUILayout.TextField("Old Bone Name", oldBoneName);
            newBoneName = EditorGUILayout.TextField("New Bone Name", newBoneName);

            if (GUILayout.Button("Select Animation Clips"))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder with Animation Clips", "", "");
                if (!string.IsNullOrEmpty(path))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    animationClips = AssetDatabase.FindAssets("t:AnimationClip", new[] { relativePath })
                        .Select(guid => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid)))
                        .ToArray();
                }
            }

            if (GUILayout.Button("Rename Bones"))
                RenameBonesInClips();

            if (GUILayout.Button("Remove Scale Properties"))
                RemoveScalePropertiesInClips();

            if (animationClips != null && animationClips.Length > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("Selected Animation Clips:", EditorStyles.boldLabel);
                foreach (var clip in animationClips)
                    GUILayout.Label(clip.name);
            }
        }

        /// <summary>
        /// Renames all occurrences of a bone name in the path of animation curves.
        /// </summary>
        void RenameBonesInClips()
        {
            if (animationClips == null || animationClips.Length == 0)
            {
                Debug.LogError("No animation clips selected.");
                return;
            }

            foreach (var clip in animationClips)
            {
                if (clip == null)
                {
                    Debug.LogError("Animation clip is null.");
                    continue;
                }

                SerializedObject serializedClip = new SerializedObject(clip);
                SerializedProperty curves = serializedClip.FindProperty("m_EditorCurves");

                if (curves == null)
                {
                    Debug.LogWarning($"No curves found in animation clip: {clip.name}");
                    continue;
                }

                bool changed = false;

                for (int i = 0; i < curves.arraySize; i++)
                {
                    SerializedProperty curve = curves.GetArrayElementAtIndex(i);
                    if (curve == null) continue;

                    SerializedProperty path = curve.FindPropertyRelative("path");
                    if (path != null && path.stringValue.Contains(oldBoneName))
                    {
                        path.stringValue = path.stringValue.Replace(oldBoneName, newBoneName);
                        changed = true;
                    }
                }

                if (changed)
                {
                    serializedClip.ApplyModifiedProperties();
                    EditorUtility.SetDirty(clip);
                    Debug.Log($"Renamed bones in animation clip: {clip.name}");
                }
            }

            Debug.Log("✅ Bone renaming completed.");
        }

        /// <summary>
        /// (Optional) Removes all scaling curves from selected animation clips to avoid double-scaling rigs.
        /// Currently commented out, but demonstrates approach using AnimationUtility.
        /// </summary>
        void RemoveScalePropertiesInClips()
        {
            /*
            if (animationClips == null || animationClips.Length == 0)
            {
                Debug.LogError("No animation clips selected.");
                return;
            }

            int removedCount = 0;
            foreach (var clip in animationClips)
            {
                if (clip == null)
                {
                    Debug.LogError("Animation clip is null.");
                    continue;
                }

                var curves = AnimationUtility.GetAllCurves(clip, true);
                foreach (var curveData in curves)
                {
                    if (curveData.propertyName.ToLower().Contains("scale"))
                    {
                        string propertyName = curveData.propertyName;
                        AnimationCurve curve = curveData.curve;

                        for (int i = curve.keys.Length - 1; i >= 0; i--)
                            curve.RemoveKey(i);

                        if (propertyName.IndexOf(".") > 0)
                            propertyName = propertyName.Substring(0, propertyName.IndexOf("."));

                        clip.SetCurve(curveData.path, curveData.type, propertyName, null);
                        removedCount++;
                        Debug.Log($"Removed scale curve: {curveData.path} - {propertyName}");
                    }
                }
            }

            Debug.Log($"✅ Total removed scale curves: {removedCount}");
            */
        }
    }
}
