using UnityEngine;
using UnityEngine.AI;

namespace EggGame.HitFeel
{
    // Отталкивание врага при ударе. Ставится на врага.
    public class KnockbackReceiver : MonoBehaviour
    {
        public enum Mode
        {
            Displacement,   // авто: NavMeshAgent -> Rigidbody -> Transform
            NavMeshAgent,   // только через agent.Move
            Rigidbody,      // только через AddForce
            Transform       // просто двигать transform
        }

        [Tooltip("Способ отталкивания. Displacement — выбрать автоматически.")]
        public Mode mode = Mode.Displacement;

        [Tooltip("Множитель силы отталкивания (тяжёлым врагам меньше).")]
        [Min(0f)] public float strengthMultiplier = 1f;

        [Tooltip("Радиус поиска ближайшей точки NavMesh после отталкивания.")]
        [Min(0.1f)] public float navMeshSnapRadius = 2f;

        private NavMeshAgent _agent;
        private Rigidbody _rb;

        private bool _active;
        private Vector3 _dir;
        private float _distance;
        private float _duration;
        private float _up;
        private float _elapsed;
        private float _prevProgress;
        private float _prevHeight;
        private bool _agentWasStopped;
        private bool _usingAgent;
        private bool _stoppedByUs;

        public bool IsKnockedBack { get { return _active; } }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _rb = GetComponent<Rigidbody>();
        }

        public void Apply(Vector3 direction, float distance, float duration, float up)
        {
            if (!isActiveAndEnabled) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f) return;
            distance *= strengthMultiplier;
            up *= strengthMultiplier;
            if (distance <= 0f && up <= 0f) return;
            if (duration < 0.01f) duration = 0.01f;

            if (_active) Finish(); // новый удар перезапускает отталкивание

            Mode m = ResolveMode();
            _dir = direction.normalized;

            if (m == Mode.Rigidbody)
            {
                // Начальная скорость для ease-out (средняя ×2), трение/drag погасит.
                Vector3 v = _dir * (2f * distance / duration);
                if (up > 0f) v += Vector3.up * Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * up);
                _rb.AddForce(v, ForceMode.VelocityChange);
                return;
            }

            _distance = distance;
            _duration = duration;
            _up = up;
            _elapsed = 0f;
            _prevProgress = 0f;
            _prevHeight = 0f;
            _usingAgent = (m == Mode.NavMeshAgent);
            _active = true;
            _stoppedByUs = false;

            if (_usingAgent && _agent.isOnNavMesh)
            {
                _stoppedByUs = true;
                _agentWasStopped = _agent.isStopped;
                _agent.isStopped = true;
            }
        }

        public void Cancel()
        {
            if (_active) Finish();
        }

        private Mode ResolveMode()
        {
            bool hasAgent = _agent != null && _agent.enabled;
            bool hasBody = _rb != null && !_rb.isKinematic;
            switch (mode)
            {
                case Mode.NavMeshAgent: return hasAgent ? Mode.NavMeshAgent : Mode.Transform;
                case Mode.Rigidbody: return hasBody ? Mode.Rigidbody : Mode.Transform;
                case Mode.Transform: return Mode.Transform;
                default:
                    if (hasAgent) return Mode.NavMeshAgent;
                    if (hasBody) return Mode.Rigidbody;
                    return Mode.Transform;
            }
        }

        private void Update()
        {
            if (!_active) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float inv = 1f - t;
            float progress = 1f - inv * inv;           // ease-out
            float height = _up * Mathf.Sin(t * Mathf.PI); // дуга вверх-вниз

            Vector3 offset = _dir * ((progress - _prevProgress) * _distance);
            _prevProgress = progress;

            if (_usingAgent)
            {
                if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                    _agent.Move(offset); // Move сам не даёт уйти с NavMesh
            }
            else
            {
                offset.y += height - _prevHeight;
                _prevHeight = height;
                transform.position += offset;
            }

            if (t >= 1f) Finish();
        }

        private void Finish()
        {
            _active = false;
            if (!_usingAgent)
            {
                // Убираем остаток подброса.
                if (_prevHeight != 0f) transform.position -= Vector3.up * _prevHeight;
                _prevHeight = 0f;
                return;
            }
            if (_agent == null || !_agent.enabled) return;

            if (!_agent.isOnNavMesh)
            {
                // Вернуть агента на ближайшую точку NavMesh.
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, navMeshSnapRadius, NavMesh.AllAreas))
                    _agent.Warp(hit.position);
            }
            if (_stoppedByUs && _agent.isOnNavMesh) _agent.isStopped = _agentWasStopped;
            _stoppedByUs = false;
        }

        private void OnDisable()
        {
            if (_active) Finish();
        }
    }
}
