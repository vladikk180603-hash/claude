using UnityEngine;
using EggGame.HitFeel;

namespace EggGame.HurtFeedback
{
    // Камера при уроне: тряска (CameraShake.Impulse) + наклон в сторону удара (CameraShake.AddOffset).
    // Камеру напрямую НЕ двигает — только через общий CameraShake из HitFeel.
    public class HurtCameraKick : MonoBehaviour
    {
        [Tooltip("CameraShake локального игрока. Если пусто — CameraShake.Local.")]
        public CameraShake cameraShake;

        private PlayerHurtFeedbackConfig _cfg;
        private float _timer;
        private float _duration;
        private float _roll;
        private float _pitch;

        public void Init(PlayerHurtFeedbackConfig cfg) { _cfg = cfg; }

        private CameraShake Shake { get { return cameraShake != null ? cameraShake : CameraShake.Local; } }

        // localSide: +1 удар справа, -1 слева. localFront: +1 спереди, -1 сзади.
        public void Kick(float intensity, float localSide, float localFront, Vector3 hitWorldDirection)
        {
            if (_cfg == null) return;
            float k = intensity * _cfg.cameraMultiplier;
            if (k <= 0f) return;

            CameraShake cs = Shake;
            if (cs != null)
            {
                cs.Impulse(_cfg.shakePosAmplitude * k, _cfg.shakeRollNoiseDeg * k, _cfg.shakeRecoil * k,
                           _cfg.shakeDuration, _cfg.shakeFrequency, hitWorldDirection);
            }

            // Наклон к стороне удара (крен вправо = отрицательный Z), удар спереди — голову назад (вверх).
            _roll = -Mathf.Clamp(localSide, -1f, 1f) * _cfg.tiltRollDeg * k;
            _pitch = -Mathf.Clamp(localFront, -1f, 1f) * _cfg.tiltPitchDeg * k;
            _duration = _cfg.tiltDuration;
            _timer = 0f;
        }

        private void Update()
        {
            if (_duration <= 0f) return;
            _timer += Time.deltaTime;
            float x = _timer / _duration;
            if (x >= 1f) { _duration = 0f; return; }

            // Быстрый наклон (первые 15%) и плавный возврат.
            float env = Mathf.Min(1f, x / 0.15f) * (1f - x) * (1f - x);
            CameraShake cs = Shake;
            if (cs != null) cs.AddOffset(Vector3.zero, new Vector3(_pitch * env, 0f, _roll * env));
        }
    }
}
