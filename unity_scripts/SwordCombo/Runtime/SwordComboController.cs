using System;
using System.Collections.Generic;
using UnityEngine;
using EggGame.HitFeel;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EggGame.SwordCombo
{
    // Серия из нескольких ударов мечом без анимационных клипов. Ставится на игрока.
    // Двигает только SwordPivot (дочерний объект камеры). Камеру не трогает напрямую —
    // покачивание отправляет в CameraShake.AddOffset.
    public class SwordComboController : MonoBehaviour
    {
        public enum State { Idle, Swing, Recovery }

        [Header("Ссылки")]
        [Tooltip("Пустой объект-«рука», дочерний от камеры. Меч (модель) лежит внутри него.")]
        public Transform swordPivot;

        [Tooltip("Удары серии по порядку. Если пусто — используются 3 встроенных удара.")]
        public SwingProfile[] swings = new SwingProfile[0];

        [Tooltip("Корень игрока (для HitInfo и чтобы не бить себя). Если пусто — этот объект.")]
        public Transform owner;

        [Tooltip("Компонент, отправляющий запрос урона (IDamageRequestSender). Если пусто — ищется на игроке.")]
        public MonoBehaviour damageSender;

        [Tooltip("Тряска камеры для покачивания при замахе. Если пусто — CameraShake.Local.")]
        public CameraShake cameraShake;

        [Header("Стойка (поза SwordPivot относительно камеры)")]
        [Tooltip("Где рука с мечом в стойке: X вправо, Y вверх, Z вперёд.")]
        public Vector3 stancePosition = new Vector3(0.32f, -0.38f, 0.5f);
        [Tooltip("Поворот меча в стойке (градусы). Клинок направлен по локальной оси +Y SwordPivot.")]
        public Vector3 stanceEuler = new Vector3(30f, 0f, 12f);

        [Header("Ввод")]
        [Tooltip("Читать левую кнопку мыши самостоятельно. Выключите, если вызываете RequestAttack() из своего ввода.")]
        public bool readMouseInput = true;
        [Tooltip("Только у своего игрока (в сети у чужих выключить).")]
        public bool isLocalPlayer = true;
        [Tooltip("Сколько секунд помнится нажатие, сделанное раньше времени.")]
        [Min(0f)] public float inputBufferSeconds = 0.4f;

        [Header("Серия")]
        [Tooltip("Если после удара прошло больше стольких секунд — серия начинается с удара 1.")]
        [Min(0f)] public float comboResetTime = 0.8f;
        [Tooltip("Пауза после последнего удара серии.")]
        [Min(0f)] public float finisherCooldown = 0.45f;
        [Tooltip("Доля начала удара, за которую меч плавно переходит из текущей позы в кривую.")]
        [Range(0.01f, 0.5f)] public float blendInFraction = 0.12f;

        [Header("Обнаружение попадания")]
        [Tooltip("Где начинается клинок по оси +Y SwordPivot (метры).")]
        public float bladeStart = 0.1f;
        [Tooltip("Где кончик клинка по оси +Y SwordPivot (метры).")]
        public float bladeEnd = 1.1f;
        [Tooltip("Необязательно: точки основания и кончика (перекрывают два поля выше).")]
        public Transform bladeBase;
        public Transform bladeTip;
        [Tooltip("Радиус проверки вокруг клинка.")]
        [Min(0.01f)] public float hitRadius = 0.15f;
        [Tooltip("Сколько сфер вдоль клинка.")]
        [Range(2, 12)] public int pointsAlongBlade = 6;
        [Tooltip("Промежуточные шаги между кадрами, чтобы быстрый клинок не «проскакивал» врага.")]
        [Range(1, 6)] public int subSteps = 3;
        [Tooltip("Слои врагов.")]
        public LayerMask hitLayers = ~0;

        [Header("Заглушка меча")]
        [Tooltip("Если в SwordPivot нет модели — создать вытянутый куб при запуске.")]
        public bool createPlaceholderIfEmpty = true;

        [Header("Отладка")]
        public bool drawGizmos = true;

        // ---- События ----
        public event Action<int> OnSwingStart;           // номер удара 0..N-1
        public event Action<HitInfo, int> OnImpact;     // попадание (для эффектов)
        public event Action<int> OnSwingEnd;             // удар закончился
        public event Action<DamageRequest> OnDamageRequested; // запрос урона (для сети)

        // ---- Состояние (для чтения) ----
        public State CurrentState { get { return _state; } }
        public int CurrentStep { get { return _step; } }
        public bool IsHitWindowOpen { get { return _windowOpen; } }
        // Локальная скорость замаха. 1 = нормально, 0 = замер. Time.timeScale не используется.
        public float SwingSpeed { get { return _swingSpeed; } }

        private State _state = State.Idle;
        private int _step;               // текущий/последний удар
        private int _nextStep;           // какой будет следующий
        private float _swingTime;        // секунды внутри удара (с учётом swingSpeed)
        private float _recoveryTime;
        private float _lastSwingEndTime = -999f;
        private float _cooldownUntil;
        private float _bufferedAt = -999f;

        private float _swingSpeed = 1f;
        private float _freezeUntil;
        private float _freezeSpeed;

        private Vector3 _fromPos;
        private Quaternion _fromRot;

        private bool _windowOpen;
        private Vector3 _prevBase;
        private Vector3 _prevTip;

        private SwingProfile[] _defaults;
        private IDamageRequestSender _sender;
        private readonly Collider[] _overlap = new Collider[32];
        private readonly HashSet<UnityEngine.Object> _hitThisSwing = new HashSet<UnityEngine.Object>();

        private void Awake()
        {
            if (owner == null) owner = transform;
            _sender = damageSender as IDamageRequestSender;
            if (_sender == null) _sender = GetComponentInChildren<IDamageRequestSender>(true);
            if (damageSender != null && !(damageSender is IDamageRequestSender))
                Debug.LogWarning("[SwordCombo] В поле Damage Sender компонент без IDamageRequestSender.", this);

            if (swordPivot == null)
            {
                Debug.LogWarning("[SwordCombo] Не задан SwordPivot — меч двигаться не будет.", this);
                return;
            }
            swordPivot.localPosition = stancePosition;
            swordPivot.localRotation = Quaternion.Euler(stanceEuler);
            if (createPlaceholderIfEmpty && swordPivot.GetComponentInChildren<Renderer>(true) == null)
                SwordPlaceholder.Build(swordPivot);
        }

        private void OnDestroy()
        {
            if (_defaults == null) return;
            for (int i = 0; i < _defaults.Length; i++)
                if (_defaults[i] != null) Destroy(_defaults[i]);
        }

        // ---------- Публичные методы ----------

        // Нажатие атаки (если ввод читается вашим скриптом).
        public void RequestAttack()
        {
            if (!isActiveAndEnabled || !isLocalPlayer) return;
            _bufferedAt = Time.time;
        }

        // Временно заморозить замах (остановка кадра). Вызывает мост HitFeel.
        public void FreezeSwing(float seconds, float speedDuringFreeze = 0f)
        {
            if (seconds <= 0f) return;
            float until = Time.time + Mathf.Min(seconds, 0.2f);
            if (until > _freezeUntil) _freezeUntil = until;
            _freezeSpeed = Mathf.Clamp01(speedDuringFreeze);
        }

        // Количество ударов в серии.
        public int SwingCount { get { return GetSwingCountInternal(); } }

        public SwingProfile GetSwing(int index)
        {
            int count = GetSwingCountInternal();
            if (count == 0) return null;
            index = Mathf.Clamp(index, 0, count - 1);
            if (swings != null && swings.Length > 0) return swings[index] != null ? swings[index] : GetDefault(index);
            return GetDefault(index);
        }

        // Сбросить серию (например, при смене оружия или смерти).
        public void ResetCombo()
        {
            if (_state == State.Swing) EndSwing(false);
            _state = State.Idle;
            _nextStep = 0;
            _bufferedAt = -999f;
            _freezeUntil = 0f;
            _cooldownUntil = 0f;
            _swingSpeed = 1f;
            if (swordPivot != null)
            {
                swordPivot.localPosition = stancePosition;
                swordPivot.localRotation = Quaternion.Euler(stanceEuler);
            }
        }

        // ---------- Цикл ----------

        private void Update()
        {
            if (swordPivot == null) return;
            ReadInput();

            float now = Time.time;
            _swingSpeed = now < _freezeUntil ? _freezeSpeed : 1f;

            switch (_state)
            {
                case State.Swing: UpdateSwing(); break;
                case State.Recovery: UpdateRecovery(); break;
            }

            // Начать удар, если есть нажатие в буфере и можно бить.
            if (_state != State.Swing && now >= _cooldownUntil && now - _bufferedAt <= inputBufferSeconds)
            {
                _bufferedAt = -999f;
                StartSwing();
            }
        }

        private void ReadInput()
        {
            if (!readMouseInput || !isLocalPlayer) return;
            if (MousePressedThisFrame()) _bufferedAt = Time.time;
        }

        private static bool MousePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse m = Mouse.current;
            return m != null && m.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        private void StartSwing()
        {
            int count = GetSwingCountInternal();
            if (count == 0) return;

            // Серия прервалась по времени — снова с первого удара.
            if (_state == State.Idle && Time.time - _lastSwingEndTime > comboResetTime) _nextStep = 0;
            if (_nextStep >= count) _nextStep = 0;

            _step = _nextStep;
            _state = State.Swing;
            _swingTime = 0f;
            _windowOpen = false;
            _hitThisSwing.Clear();
            _fromPos = swordPivot.localPosition;
            _fromRot = swordPivot.localRotation;

            if (OnSwingStart != null) OnSwingStart(_step);
        }

        private void UpdateSwing()
        {
            SwingProfile p = GetSwing(_step);
            if (p == null) { _state = State.Idle; return; }

            float dur = Mathf.Max(0.1f, p.duration);
            _swingTime += Time.deltaTime * _swingSpeed;
            float t = Mathf.Clamp01(_swingTime / dur);

            // Поза из кривых.
            Vector3 pos = stancePosition + new Vector3(p.posX.Evaluate(t), p.posY.Evaluate(t), p.posZ.Evaluate(t));
            Quaternion rot = Quaternion.Euler(stanceEuler + new Vector3(p.rotX.Evaluate(t), p.rotY.Evaluate(t), p.rotZ.Evaluate(t)));

            // Плавный вход из текущей позы (после прошлого удара).
            float blend = Smooth(t / blendInFraction);
            if (blend < 1f)
            {
                pos = Vector3.Lerp(_fromPos, pos, blend);
                rot = Quaternion.Slerp(_fromRot, rot, blend);
            }
            swordPivot.localPosition = pos;
            swordPivot.localRotation = rot;

            // Покачивание камеры — добавкой в CameraShake (сам контроллер камеру не двигает).
            CameraShake cs = cameraShake != null ? cameraShake : CameraShake.Local;
            if (cs != null)
            {
                float env = SwingProfile.CameraEnvelope(t);
                if (env > 0f)
                    cs.AddOffset(p.cameraSwayPos * env, new Vector3(p.cameraPitchDeg * env, 0f, p.cameraRollDeg * env));
            }

            // Окно удара.
            bool inWindow = t >= p.hitWindowStart && t <= p.hitWindowEnd;
            if (inWindow)
            {
                Vector3 b, tip;
                GetBlade(out b, out tip);
                if (!_windowOpen)
                {
                    _windowOpen = true;
                    _prevBase = b;
                    _prevTip = tip;
                }
                ScanBlade(_prevBase, _prevTip, b, tip, p);
                _prevBase = b;
                _prevTip = tip;
            }
            else
            {
                _windowOpen = false;
            }

            if (t >= 1f) EndSwing(true);
        }

        private void EndSwing(bool raiseEvent)
        {
            _windowOpen = false;
            _state = State.Recovery;
            _recoveryTime = 0f;
            _fromPos = swordPivot != null ? swordPivot.localPosition : stancePosition;
            _fromRot = swordPivot != null ? swordPivot.localRotation : Quaternion.Euler(stanceEuler);
            _lastSwingEndTime = Time.time;

            int count = GetSwingCountInternal();
            _nextStep = _step + 1;
            if (_nextStep >= count)
            {
                // Последний удар серии — пауза и сброс.
                _nextStep = 0;
                _cooldownUntil = Time.time + finisherCooldown;
            }
            if (raiseEvent && OnSwingEnd != null) OnSwingEnd(_step);
        }

        private void UpdateRecovery()
        {
            SwingProfile p = GetSwing(_step);
            float dur = p != null ? Mathf.Max(0.01f, p.recoveryDuration) : 0.3f;
            _recoveryTime += Time.deltaTime;
            float k = Smooth(_recoveryTime / dur);
            swordPivot.localPosition = Vector3.Lerp(_fromPos, stancePosition, k);
            swordPivot.localRotation = Quaternion.Slerp(_fromRot, Quaternion.Euler(stanceEuler), k);
            if (k >= 1f) _state = State.Idle;
        }

        // ---------- Попадания ----------

        private void GetBlade(out Vector3 basePos, out Vector3 tipPos)
        {
            basePos = bladeBase != null ? bladeBase.position : swordPivot.TransformPoint(0f, bladeStart, 0f);
            tipPos = bladeTip != null ? bladeTip.position : swordPivot.TransformPoint(0f, bladeEnd, 0f);
        }

        private void ScanBlade(Vector3 prevBase, Vector3 prevTip, Vector3 curBase, Vector3 curTip, SwingProfile p)
        {
            int points = Mathf.Max(2, pointsAlongBlade);
            int steps = Mathf.Max(1, subSteps);
            for (int s = 1; s <= steps; s++)
            {
                float f = (float)s / steps;
                Vector3 b = Vector3.Lerp(prevBase, curBase, f);
                Vector3 tip = Vector3.Lerp(prevTip, curTip, f);
                for (int i = 0; i < points; i++)
                {
                    Vector3 point = Vector3.Lerp(b, tip, (float)i / (points - 1));
                    int n = Physics.OverlapSphereNonAlloc(point, hitRadius, _overlap, hitLayers, QueryTriggerInteraction.Ignore);
                    for (int k = 0; k < n; k++)
                    {
                        Collider c = _overlap[k];
                        _overlap[k] = null;
                        if (c != null) TryRegisterHit(c, point, p);
                        if (_state != State.Swing) return; // на случай ResetCombo из обработчика
                    }
                }
            }
        }

        private void TryRegisterHit(Collider c, Vector3 samplePoint, SwingProfile p)
        {
            if (owner != null && c.transform.IsChildOf(owner)) return;      // себя не бьём
            if (c.transform.IsChildOf(swordPivot)) return;

            GameObject targetRoot = GetTargetRoot(c);
            if (!_hitThisSwing.Add(targetRoot)) return; // один враг — один раз за удар

            Vector3 contact = SafeClosestPoint(c, samplePoint);
            Vector3 dir = contact - owner.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = owner.forward;
            dir.Normalize();
            Vector3 normal = samplePoint - contact;
            normal = normal.sqrMagnitude > 1e-6f ? normal.normalized : -dir;

            HitInfo hit = new HitInfo(owner, c, contact, normal, dir);

            // 1) Локальные эффекты.
            if (OnImpact != null) OnImpact(hit, _step);

            // 2) Отдельно — запрос урона (сейчас локально, потом RPC хосту).
            DamageRequest req = DamageRequestUtil.FromHit(hit, _step, p.damageMultiplier, targetRoot);
            if (OnDamageRequested != null) OnDamageRequested(req);
            if (_sender != null) _sender.SendDamageRequest(req);
        }

        // Цель целиком (чтобы враг с несколькими коллайдерами получил урон один раз).
        private static GameObject GetTargetRoot(Collider c)
        {
            MonoBehaviour dmg = c.GetComponentInParent<IDamageable>() as MonoBehaviour;
            if (dmg != null) return dmg.gameObject;
            KnockbackReceiver kr = c.GetComponentInParent<KnockbackReceiver>();
            if (kr != null) return kr.gameObject;
            if (c.attachedRigidbody != null) return c.attachedRigidbody.gameObject;
            return c.gameObject;
        }

        private static Vector3 SafeClosestPoint(Collider c, Vector3 from)
        {
            MeshCollider mc = c as MeshCollider;
            if (mc != null && !mc.convex) return c.bounds.ClosestPoint(from);
            Vector3 p = c.ClosestPoint(from);
            if (p == from) p = c.bounds.ClosestPoint(from);
            if (p == from) p = c.bounds.center;
            return p;
        }

        // ---------- Вспомогательное ----------

        private int GetSwingCountInternal()
        {
            if (swings != null && swings.Length > 0) return swings.Length;
            return 3;
        }

        private SwingProfile GetDefault(int index)
        {
            if (_defaults == null) _defaults = new SwingProfile[3];
            index = Mathf.Clamp(index, 0, 2);
            if (_defaults[index] == null)
            {
                _defaults[index] = SwingProfile.CreateDefault(index);
                _defaults[index].hideFlags = HideFlags.DontSave;
            }
            return _defaults[index];
        }

        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        private void OnDisable()
        {
            _windowOpen = false;
            _state = State.Idle;
            if (swordPivot != null)
            {
                swordPivot.localPosition = stancePosition;
                swordPivot.localRotation = Quaternion.Euler(stanceEuler);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || swordPivot == null) return;
            Vector3 b, tip;
            GetBlade(out b, out tip);
            Gizmos.color = _windowOpen ? Color.red : Color.yellow;
            int points = Mathf.Max(2, pointsAlongBlade);
            for (int i = 0; i < points; i++)
                Gizmos.DrawWireSphere(Vector3.Lerp(b, tip, (float)i / (points - 1)), hitRadius);
            Gizmos.DrawLine(b, tip);
        }
    }
}
