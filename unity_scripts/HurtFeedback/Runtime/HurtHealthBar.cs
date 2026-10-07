using UnityEngine;
using UnityEngine.UI;

namespace EggGame.HurtFeedback
{
    // Простая полоска здоровья: дёргается при уроне, потерянный кусок белый и съезжает,
    // мигает во время неуязвимости.
    public class HurtHealthBar : MonoBehaviour
    {
        private PlayerHurtFeedbackConfig _cfg;
        private RectTransform _root;
        private RectTransform _fill;
        private RectTransform _lost;
        private Image _fillImage;
        private Image _lostImage;
        private Vector2 _basePos;

        private float _shown = 1f;      // текущая доля (красная часть)
        private float _lostFrom = 1f;   // правый край белого куска
        private float _holdTimer;
        private float _slideSpeed;
        private float _shakeTimer;
        private float _shakeAmp;
        private float _invulnTimer;
        private float _invulnTime;

        public void Init(PlayerHurtFeedbackConfig cfg, RectTransform root, RectTransform lost, RectTransform fill)
        {
            _cfg = cfg;
            _root = root;
            _lost = lost;
            _fill = fill;
            _fillImage = fill.GetComponent<Image>();
            _lostImage = lost.GetComponent<Image>();
            _basePos = root.anchoredPosition;
            SetRight(_fill, 1f);
            SetRight(_lost, 1f);
        }

        // Новая доля здоровья 0..1. Уменьшение = урон (белый кусок), увеличение = лечение.
        public void SetFraction(float fraction)
        {
            if (_cfg == null) return;
            fraction = Mathf.Clamp01(fraction);
            if (fraction < _shown)
            {
                // Белый кусок от старого значения (или от ещё не съехавшего куска).
                _lostFrom = Mathf.Max(_lostFrom, _shown);
                _holdTimer = _cfg.lostChunkHold;
                _slideSpeed = 0f;
            }
            else if (fraction > _lostFrom)
            {
                _lostFrom = fraction;
                _holdTimer = 0f;
            }
            _shown = fraction;
            SetRight(_fill, _shown);
            SetRight(_lost, Mathf.Max(_lostFrom, _shown));
        }

        public void Shake(float intensity)
        {
            if (_cfg == null) return;
            float amp = _cfg.healthBarShakePixels * intensity * _cfg.healthBarMultiplier;
            _shakeAmp = _shakeTimer > 0f ? Mathf.Max(_shakeAmp, amp) : amp;
            _shakeTimer = _cfg.healthBarShakeDuration;
        }

        public void ShowInvulnerability(float seconds)
        {
            _invulnTimer = Mathf.Max(_invulnTimer, seconds);
        }

        private void Update()
        {
            if (_cfg == null || _root == null) return;
            float dt = Time.deltaTime;

            // Белый кусок: стоит, потом съезжает за lostChunkSlide.
            if (_lostFrom > _shown)
            {
                if (_holdTimer > 0f)
                {
                    _holdTimer -= dt;
                    if (_holdTimer <= 0f) _slideSpeed = (_lostFrom - _shown) / _cfg.lostChunkSlide;
                }
                else
                {
                    if (_slideSpeed <= 0f) _slideSpeed = (_lostFrom - _shown) / _cfg.lostChunkSlide;
                    _lostFrom = Mathf.MoveTowards(_lostFrom, _shown, _slideSpeed * dt);
                }
                SetRight(_lost, _lostFrom);
            }

            // Дёргание.
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= dt;
                float k = Mathf.Clamp01(_shakeTimer / _cfg.healthBarShakeDuration);
                float t = Time.time * 60f;
                Vector2 o = new Vector2(Mathf.PerlinNoise(t, 0.3f) * 2f - 1f, Mathf.PerlinNoise(0.7f, t) * 2f - 1f);
                _root.anchoredPosition = _basePos + o * (_shakeAmp * k);
                if (_shakeTimer <= 0f) _root.anchoredPosition = _basePos;
            }

            // Мигание неуязвимости.
            Color baseColor = _cfg.healthBarColor;
            if (_invulnTimer > 0f)
            {
                _invulnTimer -= dt;
                _invulnTime += dt;
                float blink = 0.5f + 0.5f * Mathf.Sin(_invulnTime * _cfg.invulnerabilityBlinkRate * Mathf.PI * 2f);
                _fillImage.color = Color.Lerp(baseColor, _cfg.invulnerabilityTint, blink);
            }
            else
            {
                _invulnTime = 0f;
                if (_fillImage.color != baseColor) _fillImage.color = baseColor;
            }
            _lostImage.color = _cfg.healthBarLostColor;
        }

        private static void SetRight(RectTransform rt, float fraction)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
