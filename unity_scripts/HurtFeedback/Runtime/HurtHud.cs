using UnityEngine;
using UnityEngine.UI;

namespace EggGame.HurtFeedback
{
    // Создаёт кодом отдельный экранный Canvas: виньетка, дуги направления, полоска здоровья.
    // Никаких готовых префабов и картинок не нужно. Не ловит клики (не мешает остальному UI).
    public class HurtHud : MonoBehaviour
    {
        public HurtVignette Vignette { get; private set; }
        public HurtDirectionIndicators Indicators { get; private set; }
        public HurtHealthBar HealthBar { get; private set; }

        private Sprite _vignetteSprite;
        private Sprite _arcSprite;
        private Sprite _whiteSprite;

        public static HurtHud Create(PlayerHurtFeedbackConfig cfg, Transform viewer, int sortingOrder)
        {
            var go = new GameObject("[HurtHUD]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var hud = go.AddComponent<HurtHud>();
            hud.Build(cfg, viewer, sortingOrder);
            return hud;
        }

        private void Build(PlayerHurtFeedbackConfig cfg, Transform viewer, int sortingOrder)
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _vignetteSprite = HurtTextures.CreateVignette(cfg.vignetteInnerRadius);
            _arcSprite = HurtTextures.CreateArc(cfg.indicatorArcDegrees, cfg.indicatorThickness / Mathf.Max(1f, cfg.indicatorRadius));
            _whiteSprite = HurtTextures.CreateWhite();

            // a) Виньетка на весь экран.
            Image vig = NewImage("Vignette", transform, _vignetteSprite);
            Stretch(vig.rectTransform);
            Vignette = vig.gameObject.AddComponent<HurtVignette>();
            Vignette.Init(vig, cfg);

            // b) Контейнер дуг в центре экрана.
            var arcsGo = new GameObject("DirectionIndicators", typeof(RectTransform));
            var arcsRt = (RectTransform)arcsGo.transform;
            arcsRt.SetParent(transform, false);
            Stretch(arcsRt);
            Indicators = arcsGo.AddComponent<HurtDirectionIndicators>();
            Indicators.Init(cfg, _arcSprite, viewer);

            // g) Полоска здоровья (по желанию).
            if (cfg.createHealthBar)
            {
                Image bg = NewImage("HealthBar", transform, _whiteSprite);
                RectTransform rt = bg.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = Vector2.zero;
                rt.sizeDelta = cfg.healthBarSize;
                rt.anchoredPosition = cfg.healthBarOffset;
                bg.color = cfg.healthBarBackground;

                Image lost = NewImage("Lost", rt, _whiteSprite);
                lost.color = cfg.healthBarLostColor;
                Image fill = NewImage("Fill", rt, _whiteSprite);
                fill.color = cfg.healthBarColor;

                HealthBar = bg.gameObject.AddComponent<HurtHealthBar>();
                HealthBar.Init(cfg, rt, lost.rectTransform, fill.rectTransform);
            }
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            DestroySprite(_vignetteSprite);
            DestroySprite(_arcSprite);
            DestroySprite(_whiteSprite);
        }

        private static void DestroySprite(Sprite s)
        {
            if (s == null) return;
            if (s.texture != null) Destroy(s.texture);
            Destroy(s);
        }
    }
}
