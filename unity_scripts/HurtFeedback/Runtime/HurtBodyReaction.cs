using UnityEngine;

namespace EggGame.HurtFeedback
{
    // Тело при уроне: короткий отброс назад от источника и «замирание» ввода.
    // Двигает через CharacterController.Move (если есть), иначе Rigidbody, иначе transform.
    public class HurtBodyReaction : MonoBehaviour
    {
        [Tooltip("Не двигать игрока самому — только отдавать CurrentKnockbackVelocity вашему контроллеру движения.")]
        public bool externalControllerMode = false;

        // Множитель ввода игрока: 1 обычно, 0 во время замирания. Контроллер движения умножает свой ввод на это.
        public float InputMultiplier { get; private set; }
        // Текущая скорость отброса (м/с) — для внешнего контроллера.
        public Vector3 CurrentKnockbackVelocity { get; private set; }

        private PlayerHurtFeedbackConfig _cfg;
        private CharacterController _cc;
        private Rigidbody _rb;

        private Vector3 _dir;
        private float _distance;
        private float _duration;
        private float _elapsed;
        private float _prevProgress;
        private float _freezeUntil;
        private float _freezeMul;

        public void Init(PlayerHurtFeedbackConfig cfg)
        {
            _cfg = cfg;
            _cc = GetComponent<CharacterController>();
            _rb = GetComponent<Rigidbody>();
            InputMultiplier = 1f;
        }

        public void Knockback(Vector3 awayDirection, float intensity)
        {
            if (_cfg == null) return;
            awayDirection.y = 0f;
            float dist = _cfg.knockbackDistance * intensity * _cfg.bodyMultiplier;
            if (awayDirection.sqrMagnitude < 1e-6f || dist <= 0f) return;
            _dir = awayDirection.normalized;
            _distance = dist;
            _duration = _cfg.knockbackDuration;
            _elapsed = 0f;
            _prevProgress = 0f;
        }

        public void Freeze(float intensity)
        {
            if (_cfg == null || _cfg.freezeSeconds <= 0f || _cfg.bodyMultiplier <= 0f) return;
            float until = Time.time + _cfg.freezeSeconds;
            if (until > _freezeUntil) _freezeUntil = until;
            _freezeMul = Mathf.Lerp(1f, _cfg.freezeInputMultiplier, _cfg.bodyMultiplier);
        }

        private void Update()
        {
            InputMultiplier = Time.time < _freezeUntil ? _freezeMul : 1f;

            if (_duration <= 0f) { CurrentKnockbackVelocity = Vector3.zero; return; }

            float dt = Time.deltaTime;
            _elapsed += dt;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float inv = 1f - t;
            float progress = 1f - inv * inv; // быстро в начале, мягко в конце
            Vector3 delta = _dir * ((progress - _prevProgress) * _distance);
            _prevProgress = progress;
            CurrentKnockbackVelocity = dt > 0f ? delta / dt : Vector3.zero;

            if (!externalControllerMode)
            {
                if (_cc != null) { if (_cc.enabled) _cc.Move(delta); }
                else if (_rb != null) _rb.MovePosition(_rb.position + delta);
                else transform.position += delta;
            }

            if (t >= 1f) { _duration = 0f; CurrentKnockbackVelocity = Vector3.zero; }
        }
    }
}
