using UnityEngine;

namespace EggGame.SwordCombo
{
    // Заглушка меча из кубов (~1.3 м). Точка хвата = начало координат SwordPivot,
    // клинок вдоль локальной оси +Y, ширина лезвия — по оси X.
    public static class SwordPlaceholder
    {
        public const string RootName = "SwordModel_Placeholder";

        public static GameObject Build(Transform pivot, bool editorImmediate = false)
        {
            if (pivot == null) return null;
            GameObject root = new GameObject(RootName);
            root.transform.SetParent(pivot, false);

            Color blade = new Color(0.22f, 0.12f, 0.32f);  // тёмно-фиолетовый
            Color metal = new Color(0.45f, 0.45f, 0.5f);
            Color grip = new Color(0.15f, 0.1f, 0.2f);

            // Рукоять: от -0.22 до 0.03
            Part(root.transform, "Handle", new Vector3(0f, -0.095f, 0f), new Vector3(0.035f, 0.25f, 0.035f), grip, editorImmediate);
            // Гарда
            Part(root.transform, "Guard", new Vector3(0f, 0.045f, 0f), new Vector3(0.26f, 0.035f, 0.05f), metal, editorImmediate);
            // Клинок: от 0.06 до 1.08 (≈1 м)
            Part(root.transform, "Blade", new Vector3(0f, 0.57f, 0f), new Vector3(0.07f, 1.02f, 0.015f), blade, editorImmediate);
            return root;
        }

        private static void Part(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, bool immediate)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            // Коллайдер не нужен: попадания считает SwordComboController.
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (immediate) Object.DestroyImmediate(col);
                else Object.Destroy(col);
            }

            // Цвет без создания нового материала (URP Lit: _BaseColor, Built-in: _Color).
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && !immediate)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                r.SetPropertyBlock(block);
            }
            if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
