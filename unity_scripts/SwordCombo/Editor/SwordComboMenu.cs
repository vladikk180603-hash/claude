using UnityEditor;
using UnityEngine;

namespace EggGame.SwordCombo.EditorTools
{
    // Меню Tools → Egg Game → Sword Combo.
    public static class SwordComboMenu
    {
        private const string Folder = "Assets/Scripts/SwordCombo/Profiles";

        [MenuItem("Tools/Egg Game/Sword Combo/Create 3 Swing Profiles")]
        public static void CreateProfiles()
        {
            EnsureFolder(Folder);
            Object last = null;
            for (int i = 0; i < 3; i++)
            {
                SwingProfile p = SwingProfile.CreateDefault(i);
                string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + p.name + ".asset");
                AssetDatabase.CreateAsset(p, path);
                last = p;
                Debug.Log("[SwordCombo] Создан профиль удара: " + path);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = last;
        }

        // Создаёт SwingSwayRoot над камерой и SwordPivot с мечом-заглушкой под камерой.
        [MenuItem("Tools/Egg Game/Sword Combo/Create Sword Pivot Under Camera")]
        public static void CreatePivot()
        {
            Camera cam = null;
            if (Selection.activeGameObject != null) cam = Selection.activeGameObject.GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Sword Combo", "Выделите камеру игрока в Hierarchy и повторите.", "OK");
                return;
            }

            // Объект покачивания между камерой и её родителем (CameraShakeRoot). Камера остаётся на месте.
            if (cam.transform.parent == null || cam.transform.parent.GetComponent<SwingCameraSway>() == null)
            {
                GameObject sway = new GameObject("SwingSwayRoot");
                Undo.RegisterCreatedObjectUndo(sway, "Create SwingSwayRoot");
                Undo.SetTransformParent(sway.transform, cam.transform.parent, "Create SwingSwayRoot");
                sway.transform.localPosition = Vector3.zero;
                sway.transform.localRotation = Quaternion.identity;
                sway.transform.localScale = Vector3.one;
                sway.transform.SetSiblingIndex(cam.transform.GetSiblingIndex());
                Undo.SetTransformParent(cam.transform, sway.transform, "Move Camera Under SwingSwayRoot");
                Undo.AddComponent<SwingCameraSway>(sway);
            }

            GameObject pivot = new GameObject("SwordPivot");
            Undo.RegisterCreatedObjectUndo(pivot, "Create SwordPivot");
            pivot.transform.SetParent(cam.transform, false);
            pivot.transform.localPosition = new Vector3(0.32f, -0.38f, 0.5f);
            pivot.transform.localRotation = Quaternion.Euler(30f, 0f, 12f);
            SwordPlaceholder.Build(pivot.transform, true);

            if (cam.nearClipPlane > 0.05f)
            {
                Undo.RecordObject(cam, "Near Clip");
                cam.nearClipPlane = 0.05f; // иначе близкий меч обрезается
                Debug.Log("[SwordCombo] Near Clip Plane камеры уменьшен до 0.05.");
            }
            Selection.activeGameObject = pivot;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
