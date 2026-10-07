using System;
using UnityEngine;

namespace EggGame.HurtFeedback
{
    // Координатор эффектов «меня ударили». Ставится на корень ЛОКАЛЬНОГО игрока.
    // Сам урон не считает: код здоровья (или RPC от хоста) вызывает OnPlayerDamaged / SetHealth.
    public class PlayerHurtFeedback : MonoBehaviour, IPlayerDamageFeedback
    {
        [Tooltip("Настройки эффектов.")]
        public PlayerHurtFeedbackConfig config;

        [Tooltip("Камера игрока (для направления дуг и наклона). Если пусто — Camera.main.")]
        public Camera playerCamera;

        [Tooltip("Это мой (локальный) игрок. У чужих игроков выключить — эффекты не играют.")]
        public bool isLocalPlayer = true;

        [Tooltip("Порядок отрисовки экранного Canvas (больше — поверх остального UI).")]
        public int hudSortingOrder = 500;

        [Header("Эффекты (можно выключить по отдельности)")]
        public bool useVignette = true;
        public bool useDirectionIndicator = true;
        public bool useCamera = true;
        public bool useBody = true;
        public bool useAudio = true;
        public bool useHealthBar = true;

        // Для других систем: (урон, макс.здоровье, позиция источника, интенсивность).
        public event Action<float, float, Vector3, float> OnHurtFeedbackPlayed;

        public static PlayerHurtFeedback Local { get; private set; }

        // Множитель ввода игрока (0 на время «замирания»). Контроллер движения умножает на него ввод.
        public float InputMultiplier { get { return _body != null ? _body.InputMultiplier : 1f; } }
        public HurtBodyReaction Body { get { return _body; } }
        public float CurrentHealth { get { return _health; } }
        public float MaxHealth { get { return _maxHealth; } }

        private HurtHud _hud;
        private HurtCameraKick _camKick;
        private HurtBodyReaction _body;
        private HurtAudio _audio;

        private float _health;
        private float _maxHealth;
        private bool _externalHealth;   // SetHealth вызывался — доверяем ему
        private float _lastHeavyEffects = -999f;
        private bool _ready;

        private void Awake()
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PlayerHurtFeedbackConfig>();
                config.hideFlags = HideFlags.DontSave;
                Debug.LogWarning("[HurtFeedback] Не задан Config — используются значения по умолчанию.", this);
            }
            _maxHealth = config.defaultMaxHealth;
            _health = _maxHealth;
        }

        private void OnEnable()
        {
            if (isLocalPlayer) Local = this;
        }

        private void OnDisable()
        {
            if (Local == this) Local = null;
        }

        private void Start()
        {
            if (isLocalPlayer) Setup();
        }

        private void OnDestroy()
        {
            if (_hud != null) Destroy(_hud.gameObject);
        }

        // Создаёт эффекты. Вызывается в Start; можно вызвать позже, если isLocalPlayer стал true.
        public void Setup()
        {
            if (_ready) return;
            _ready = true;
            Transform viewer = GetViewer();

            if (useVignette || useDirectionIndicator || useHealthBar)
            {
                _hud = HurtHud.Create(config, viewer, hudSortingOrder);
                if (!useVignette && _hud.Vignette != null) _hud.Vignette.gameObject.SetActive(false);
                if (!useHealthBar && _hud.HealthBar != null) _hud.HealthBar.gameObject.SetActive(false);
            }
            if (useCamera)
            {
                _camKick = GetComponent<HurtCameraKick>();
                if (_camKick == null) _camKick = gameObject.AddComponent<HurtCameraKick>();
                _camKick.Init(config);
            }
            if (useBody)
            {
                _body = GetComponent<HurtBodyReaction>();
                if (_body == null) _body = gameObject.AddComponent<HurtBodyReaction>();
                _body.Init(config);
            }
            if (useAudio)
            {
                _audio = GetComponent<HurtAudio>();
                if (_audio == null) _audio = gameObject.AddComponent<HurtAudio>();
                _audio.Init(config);
            }
            RefreshHealthVisuals();
        }

        // ---------- Точки входа (IPlayerDamageFeedback) ----------

        public void OnPlayerDamaged(float damage, float maxHealth, Vector3 sourceWorldPosition)
        {
            if (!isLocalPlayer || !isActiveAndEnabled || damage <= 0f) return;
            if (!_ready) Setup();

            if (maxHealth > 0f) _maxHealth = maxHealth;
            if (!_externalHealth) _health = Mathf.Max(0f, _health - damage);

            float intensity = config.ComputeIntensity(damage, _maxHealth);

            // Направление удара относительно взгляда игрока.
            Transform viewer = GetViewer();
            Vector3 origin = viewer != null ? viewer.position : transform.position;
            Vector3 toSource = sourceWorldPosition - origin;
            toSource.y = 0f;
            Vector3 fwd = viewer != null ? viewer.forward : transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
            bool hasDir = toSource.sqrMagnitude > 1e-4f;
            Vector3 dirToSource = hasDir ? toSource.normalized : fwd;
            float side = Vector3.Dot(dirToSource, right);
            float front = Vector3.Dot(dirToSource, fwd);

            // Лёгкие эффекты — каждый удар (виньетка складывается до потолка, дуг не больше N).
            if (_hud != null)
            {
                if (useVignette && _hud.Vignette != null) _hud.Vignette.Flash(intensity);
                if (useDirectionIndicator && hasDir && _hud.Indicators != null) _hud.Indicators.Show(sourceWorldPosition, intensity);
                if (useHealthBar && _hud.HealthBar != null) _hud.HealthBar.Shake(intensity);
            }
            if (_audio != null) _audio.PlayHurt(intensity);

            // Тяжёлые эффекты — не чаще effectMinInterval.
            if (Time.time - _lastHeavyEffects >= config.effectMinInterval)
            {
                _lastHeavyEffects = Time.time;
                if (_camKick != null) _camKick.Kick(intensity, side, front, -dirToSource);
                if (_body != null)
                {
                    _body.Knockback(-dirToSource, intensity);
                    _body.Freeze(intensity);
                }
            }

            if (config.showInvulnerabilityOnDamage) ShowInvulnerability(config.invulnerabilitySeconds);
            RefreshHealthVisuals();

            if (OnHurtFeedbackPlayed != null) OnHurtFeedbackPlayed(damage, _maxHealth, sourceWorldPosition, intensity);
        }

        public void SetHealth(float currentHealth, float maxHealth)
        {
            _externalHealth = true;
            if (maxHealth > 0f) _maxHealth = maxHealth;
            _health = Mathf.Clamp(currentHealth, 0f, _maxHealth);
            if (!_ready && isLocalPlayer) Setup();
            RefreshHealthVisuals();
        }

        public void ShowInvulnerability(float seconds)
        {
            if (seconds <= 0f || _hud == null || _hud.HealthBar == null) return;
            _hud.HealthBar.ShowInvulnerability(seconds);
        }

        // ---------- Вспомогательное ----------

        private void RefreshHealthVisuals()
        {
            float frac = _maxHealth > 0f ? _health / _maxHealth : 1f;
            float low = 0f;
            if (frac < config.lowHealthThreshold && config.lowHealthThreshold > 0f)
                low = Mathf.Lerp(0.4f, 1f, 1f - frac / config.lowHealthThreshold);

            if (_hud != null)
            {
                if (_hud.Vignette != null) _hud.Vignette.SetLowHealth(low);
                if (_hud.HealthBar != null) _hud.HealthBar.SetFraction(frac);
            }
            if (_audio != null) _audio.SetLowHealth(low);
        }

        private Transform GetViewer()
        {
            Camera c = playerCamera != null ? playerCamera : Camera.main;
            if (c != null && _hud != null && _hud.Indicators != null) _hud.Indicators.SetViewer(c.transform);
            return c != null ? c.transform : transform;
        }
    }
}
