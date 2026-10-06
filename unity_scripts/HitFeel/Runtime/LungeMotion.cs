using UnityEngine;

namespace EggGame.HitFeel
{
    // Рывок игрока вперёд при замахе. Ставится на игрока.
    public class LungeMotion : MonoBehaviour
    {
        [Tooltip("Если включено, сам игрока не двигает — только считает CurrentLungeVelocity, а ваш контроллер добавляет её к своему движению.")]
        public bool externalControllerMode = false;

        private CharacterController _cc;
        private Rigidbody _rb;

        private bool _active;
        private Vector3 _dir;
        private float _v0;
        private float _damping;
        private float _duration;
        private float _elapsed;
        private Vector3 _appliedRbVelocity;

        // Текущая скорость рывка (м/с, горизонтальная).
        public Vector3 CurrentLungeVelocity { get; private set; }
        public bool IsLunging { get { return _active; } }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _rb = GetComponent<Rigidbody>();
        }

        public void Begin(Vector3 direction, float distance, float duration, float damping)
        {
            if (!isActiveAndEnabled) return;
            direction.y = 0f; // только по горизонтали, без прыжка вверх
            if (direction.sqrMagnitude < 1e-6f || distance <= 0f) return;
            if (_active) Cancel();

            _dir = direction.normalized;
            _duration = Mathf.Max(0.01f, duration);
            _damping = Mathf.Max(0.01f, damping);
            // v(t) = v0 * e^(-k t); путь за duration = distance
            float denom = 1f - Mathf.Exp(-_damping * _duration);
            _v0 = distance * _damping / Mathf.Max(denom, 1e-4f);
            _elapsed = 0f;
            _appliedRbVelocity = Vector3.zero;
            _active = true;
            CurrentLungeVelocity = _dir * _v0;
        }

        public void Cancel()
        {
            if (!_active) return;
            _active = false;
            if (_rb != null && !_rb.isKinematic && !externalControllerMode && _cc == null)
                _rb.AddForce(-_appliedRbVelocity, ForceMode.VelocityChange);
            _appliedRbVelocity = Vector3.zero;
            CurrentLungeVelocity = Vector3.zero;
        }

        private bool UseRigidbody
        {
            get { return _cc == null && _rb != null && !_rb.isKinematic; }
        }

        private void Update()
        {
            if (!_active || UseRigidbody) return;
            if (!Step(Time.deltaTime)) return;
            if (externalControllerMode) return;

            Vector3 delta = CurrentLungeVelocity * Time.deltaTime;
            if (_cc != null)
            {
                if (_cc.enabled) _cc.Move(delta);
            }
            else if (_rb != null) // кинематический Rigidbody
            {
                _rb.MovePosition(_rb.position + delta);
            }
            else
            {
                transform.position += delta;
            }
        }

        private void FixedUpdate()
        {
            if (!_active || !UseRigidbody) return;
            if (!Step(Time.fixedDeltaTime)) return;
            if (externalControllerMode) return;

            // Добавляем только разницу скорости, чтобы не мешать остальной физике.
            Vector3 target = CurrentLungeVelocity;
            _rb.AddForce(target - _appliedRbVelocity, ForceMode.VelocityChange);
            _appliedRbVelocity = target;
        }

        // Обновляет скорость; false — если рывок закончился.
        private bool Step(float dt)
        {
            _elapsed += dt;
            if (_elapsed >= _duration)
            {
                Cancel();
                return false;
            }
            CurrentLungeVelocity = _dir * (_v0 * Mathf.Exp(-_damping * _elapsed));
            return true;
        }

        private void OnDisable() { Cancel(); }
    }
}
