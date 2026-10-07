using UnityEngine;
using UnityEngine.UI;

namespace EggGame.HurtFeedback
{
    // Красная виньетка по краям экрана: вспышка при уроне + пульс при малом здоровье.
    public class HurtVignette : MonoBehaviour
    {
        private Image _image;
        private PlayerHurtFeedbackConfig _cfg;

        private float _level;      // текущая яркость вспышки
        private float _target;     // к чему растём
        private bool _rising;
        private float _decayRate;
        private float _lowHealth;  // 0..1 насколько «мало здоровья»
        private float _pulseTime;

        public void Init(Image image, PlayerHurtFeedbackConfig cfg)
        {
            _image = image;
            _cfg = cfg;
            Apply(0f);
        }

        // Вспышки складываются до потолка.
        public void Flash(float intensity)
        {
            if (_cfg == null || _image == null) return;
            float peak = _cfg.vignetteMaxAlpha * intensity * _cfg.vignetteMultiplier;
            if (peak <= 0f) return;
            _target = Mathf.Min(Mathf.Max(_level, _target) + peak, _cfg.vignetteCap);
            _rising = true;
        }

        // 0 = здоровья достаточно, 1 = почти мёртв.
        public void SetLowHealth(float factor01)
        {
            _lowHealth = Mathf.Clamp01(factor01);
        }

        private void Update()
        {
            if (_cfg == null || _image == null) return;
            float dt = Time.deltaTime;

            if (_rising)
            {
                _level = Mathf.MoveTowards(_level, _target, dt * _cfg.vignetteCap / _cfg.vignetteAttack);
                if (_level >= _target - 1e-4f)
                {
                    _rising = false;
                    _decayRate = Mathf.Max(_level, 0.01f) / _cfg.vignetteFade;
                    _target = 0f;
                }
            }
            else if (_level > 0f)
            {
                _level = Mathf.MoveTowards(_level, 0f, dt * _decayRate);
            }

            float pulse = 0f;
            if (_lowHealth > 0f)
            {
                _pulseTime += dt;
                // Удар сердца: резкий подъём и спад, ~1 раз в секунду.
                float phase = Mathf.Repeat(_pulseTime * _cfg.lowHealthPulseRate, 1f);
                float beat = Mathf.Exp(-phase * 6f);
                pulse = _cfg.lowHealthPulseAlpha * _cfg.vignetteMultiplier * _lowHealth * (0.35f + 0.65f * beat);
            }
            else
            {
                _pulseTime = 0f;
            }

            Apply(Mathf.Min(_level + pulse, 1f));
        }

        private void Apply(float alpha)
        {
            if (_image == null) return;
            Color c = _cfg != null ? _cfg.vignetteColor : Color.red;
            c.a = alpha;
            _image.color = c;
            bool show = alpha > 0.001f;
            if (_image.enabled != show) _image.enabled = show;
        }
    }
}
