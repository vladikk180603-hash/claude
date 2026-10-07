using UnityEditor;
using UnityEngine;

namespace EggGame.HurtFeedback.EditorTools
{
    // Меню Tools → Egg Game → Hurt Feedback → Create Config.
    public static class HurtFeedbackMenu
    {
        private const string Folder = "Assets/Scripts/HurtFeedback/Config";

        [MenuItem("Tools/Egg Game/Hurt Feedback/Create Config")]
        public static void CreateConfig()
        {
            EnsureFolder(Folder);
            var cfg = ScriptableObject.CreateInstance<PlayerHurtFeedbackConfig>();
            string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/PlayerHurtFeedbackConfig.asset");
            AssetDatabase.CreateAsset(cfg, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = cfg;
            Debug.Log("[HurtFeedback] Создан конфиг: " + path);
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
