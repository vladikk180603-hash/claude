using UnityEditor;
using UnityEngine;

namespace EggGame.HitFeel.EditorTools
{
    // Меню Tools → Egg Game → Hit Feel: создание готовых профилей.
    public static class HitFeelProfileMenu
    {
        private const string Folder = "Assets/Scripts/HitFeel/Profiles";

        [MenuItem("Tools/Egg Game/Hit Feel/Create Sword Profile")]
        public static void CreateSword()
        {
            HitFeelProfile p = ScriptableObject.CreateInstance<HitFeelProfile>(); // значения по умолчанию = меч
            Save(p, "Sword_HitFeel");
        }

        [MenuItem("Tools/Egg Game/Hit Feel/Create Heavy Profile")]
        public static void CreateHeavy()
        {
            HitFeelProfile p = ScriptableObject.CreateInstance<HitFeelProfile>();
            p.hitStopSeconds = 0.09f;
            p.attackerAnimSpeedDuringStop = 0f;
            p.targetAnimSpeedDuringStop = 0.02f;
            p.knockbackDistance = 2f;
            p.knockbackDuration = 0.28f;
            p.knockbackUp = 0.15f;
            p.shakePosAmplitude = 0.06f;
            p.shakeRollAmplitudeDeg = 2.5f;
            p.shakeRecoilBack = 0.1f;
            p.shakeDuration = 0.26f;
            p.shakeFrequency = 22f;
            p.lungeDistance = 1.1f;
            p.lungeDuration = 0.24f;
            p.lungeDamping = 6f;
            Save(p, "Heavy_HitFeel");
        }

        [MenuItem("Tools/Egg Game/Hit Feel/Create Light Profile")]
        public static void CreateLight()
        {
            HitFeelProfile p = ScriptableObject.CreateInstance<HitFeelProfile>();
            p.hitStopSeconds = 0.035f;
            p.attackerAnimSpeedDuringStop = 0.1f;
            p.targetAnimSpeedDuringStop = 0.1f;
            p.knockbackDistance = 0.6f;
            p.knockbackDuration = 0.15f;
            p.knockbackUp = 0f;
            p.shakePosAmplitude = 0.015f;
            p.shakeRollAmplitudeDeg = 0.6f;
            p.shakeRecoilBack = 0.03f;
            p.shakeDuration = 0.12f;
            p.shakeFrequency = 34f;
            p.lungeDistance = 0.5f;
            p.lungeDuration = 0.12f;
            p.lungeDamping = 10f;
            Save(p, "Light_HitFeel");
        }

        private static void Save(HitFeelProfile p, string name)
        {
            EnsureFolder(Folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + name + ".asset");
            AssetDatabase.CreateAsset(p, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = p;
            Debug.Log("[HitFeel] Создан профиль: " + path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
