using UnityEngine;

namespace EggGame.HurtFeedback
{
    // Картинки для эффектов рисуются кодом — внешние текстуры не нужны.
    public static class HurtTextures
    {
        // Радиальная маска: прозрачный центр, белые края. Цвет задаётся Image.color.
        public static Sprite CreateVignette(float innerRadius, int size = 256)
        {
            var tex = NewTexture(size, "HurtVignette");
            var px = new Color32[size * size];
            float inner = Mathf.Clamp(innerRadius, 0.05f, 0.95f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);            // 0 в центре, 1 у края, 1.41 в углу
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, 1.2f, r));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // Дуга вверху квадрата (смотрит «вперёд»). thickness01 — толщина в долях радиуса.
        public static Sprite CreateArc(float arcDegrees, float thickness01, int size = 256)
        {
            var tex = NewTexture(size, "HurtArc");
            var px = new Color32[size * size];
            float half = Mathf.Clamp(arcDegrees, 10f, 180f) * 0.5f;
            float outer = 0.98f;
            float inner = Mathf.Clamp(outer - Mathf.Clamp(thickness01, 0.02f, 0.9f), 0.05f, outer - 0.01f);
            float edge = 2.5f / size; // мягкий край
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float ang = Mathf.Abs(Mathf.Atan2(u, v) * Mathf.Rad2Deg); // 0 = вверх
                    float radial = Mathf.Clamp01((r - inner) / edge) * Mathf.Clamp01((outer - r) / edge);
                    float angular = Mathf.Clamp01((half - ang) / 12f);          // плавные концы дуги
                    float center = 1f - 0.35f * (ang / half);                   // ярче в середине
                    float a = radial * angular * Mathf.Clamp01(center);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // Белый квадрат 4×4 для простых прямоугольников UI.
        public static Sprite CreateWhite()
        {
            var tex = NewTexture(4, "HurtWhite");
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D NewTexture(int size, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }
    }
}
