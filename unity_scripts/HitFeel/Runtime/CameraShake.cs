using UnityEngine;

namespace EggGame.HitFeel
{
    // Тряска камеры. Ставится на ОТДЕЛЬНЫЙ пустой объект между игроком/головой и камерой.
    // Управляет localPosition/localRotation только этого объекта.
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("Максимальная накопленная сила при наложении нескольких импульсов (1 = один удар).")]
        [Min(1f)] public float maxStackedStrength = 1.6f;

        [Tooltip("Общий множитель тряски (например, для настройки в меню).")]
        [Range(0f, 2f)] public float globalMultiplier = 1f;

        [Tooltip("Ставить только на камеру ЛОКАЛЬНОГО игрока. Если включено, этот экземпляр становится CameraShake.Local.")]
        public bool registerAsLocal = true;

        // Экземпляр тряски локального игрока.
        public static CameraShake Local { get; private set; }

        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;

        private float _strength;      // 0..maxStackedStrength
        private float _posAmp;
        private float _rollAmp;
        private float _recoil;
        private float _duration = 0.18f;
        private float _frequency = 28f;
        private float _sideBias;      // смещение вбок по направлению удара
        private float _seed;
        private float _time;

        // Внешняя добавка (например, покачивание от замаха). Собирается за кадр и сбрасывается.
        private Vector3 _extraPos;
        private Vector3 _extraEuler;
        private bool _dirty;

        private void Awake()
        {
            _baseLocalPos = transform.localPosition;
            _baseLocalRot = transform.localRotation;
            _seed = Random.value * 100f;
        }

        private void OnEnable()
        {
            if (registerAsLocal) Local = this;
        }

        private void OnDisable()
        {
            if (Local == this) Local = null;
            _strength = 0f;
            _extraPos = Vector3.zero;
            _extraEuler = Vector3.zero;
            _dirty = false;
            transform.localPosition = _baseLocalPos;
            transform.localRotation = _baseLocalRot;
        }

        // Удобный статический вызов для локальной камеры.
        public static void ImpulseLocal(HitFeelProfile profile, Vector3 worldDirection)
        {
            if (Local != null) Local.Impulse(profile, worldDirection);
        }

        public void Impulse(HitFeelProfile profile, Vector3 worldDirection)
        {
            if (profile == null) return;
            Impulse(profile.shakePosAmplitude, profile.shakeRollAmplitudeDeg, profile.shakeRecoilBack,
                    profile.shakeDuration, profile.shakeFrequency, worldDirection);
        }

        public void Impulse(float posAmplitude, float rollDeg, float recoilBack, float duration, float frequency, Vector3 worldDirection)
        {
            // Берём самые сильные параметры из активных импульсов, силу складываем.
            _posAmp = Mathf.Max(_strength > 0f ? _posAmp : 0f, posAmplitude);
            _rollAmp = Mathf.Max(_strength > 0f ? _rollAmp : 0f, rollDeg);
            _recoil = Mathf.Max(_strength > 0f ? _recoil : 0f, recoilBack);
            _duration = Mathf.Max(0.01f, duration);
            _frequency = Mathf.Max(0.1f, frequency);
            _strength = Mathf.Min(_strength + 1f, maxStackedStrength);
            _time = 0f;

            Vector3 parentSpace = transform.parent != null
                ? transform.parent.InverseTransformDirection(worldDirection)
                : worldDirection;
            _sideBias = Mathf.Clamp(parentSpace.x, -1f, 1f);
        }

        // Добавка к позиции/повороту на ЭТОТ кадр (в локальных осях объекта).
        // Вызывать каждый кадр из Update, пока нужна. Несколько вызовов складываются.
        public void AddOffset(Vector3 localPositionOffset, Vector3 localEulerOffset)
        {
            _extraPos += localPositionOffset;
            _extraEuler += localEulerOffset;
        }

        private void LateUpdate()
        {
            bool hasExtra = _extraPos != Vector3.zero || _extraEuler != Vector3.zero;
            if (_strength <= 0f && !hasExtra)
            {
                if (_dirty)
                {
                    transform.localPosition = _baseLocalPos;
                    transform.localRotation = _baseLocalRot;
                    _dirty = false;
                }
                return;
            }

            Vector3 offset = Vector3.zero;
            float roll = 0f;

            if (_strength > 0f)
            {
                float dt = Time.deltaTime;
                _time += dt;
                _strength = Mathf.Max(0f, _strength - dt / _duration);

                float s = _strength * globalMultiplier;
                float s2 = s * s; // квадрат даёт мягкий хвост
                float n = _time * _frequency;

                // Шум Перлина в диапазоне -1..1
                float nx = Mathf.PerlinNoise(_seed, n) * 2f - 1f;
                float ny = Mathf.PerlinNoise(_seed + 17.3f, n) * 2f - 1f;
                float nr = Mathf.PerlinNoise(_seed + 41.7f, n) * 2f - 1f;

                offset.x = (nx + _sideBias * 0.5f) * _posAmp * s2;
                offset.y = ny * _posAmp * s2;
                offset.z = -_recoil * Mathf.Min(s, 1f) * Mathf.Min(s, 1f); // отдача назад
                roll = nr * _rollAmp * s2;
            }

            transform.localPosition = _baseLocalPos + offset + _extraPos;
            transform.localRotation = _baseLocalRot * Quaternion.Euler(_extraEuler.x, _extraEuler.y, _extraEuler.z + roll);
            _dirty = true;

            _extraPos = Vector3.zero;
            _extraEuler = Vector3.zero;
        }
    }
}
