using UnityEngine;
using EggGame.HitFeel;

namespace EggGame.SwordCombo
{
    // Связка серии ударов с HitFeel. Ставится на игрока рядом с SwordComboController и HitFeelController.
    // Подписывается на OnSwingStart / OnImpact и запускает эффекты HitFeel с множителями из SwingProfile.
    public class ComboHitFeelBridge : MonoBehaviour
    {
        [Tooltip("Серия ударов. Если пусто — ищется на этом объекте.")]
        public SwordComboController combo;

        [Tooltip("HitFeelController. Если пусто — ищется на этом объекте.")]
        public HitFeelController hitFeel;

        [Tooltip("Базовый профиль HitFeel. Если пусто — берётся профиль из HitFeelController.")]
        public HitFeelProfile baseProfile;

        [Tooltip("Замораживать замах меча на время остановки кадра.")]
        public bool freezeSwingOnHit = true;

        [Tooltip("Скорость замаха во время остановки кадра (0 = полностью замер).")]
        [Range(0f, 1f)] public float swingSpeedDuringStop = 0f;

        [Tooltip("Делать рывок вперёд в начале каждого удара.")]
        public bool lungeOnSwing = true;

        // Копии профиля для каждого удара (создаются один раз, без мусора на каждый удар).
        private HitFeelProfile[] _scaled;
        private HitFeelProfile _scaledSource;
        private SwordComboController _subscribed;

        private void Awake()
        {
            if (combo == null) combo = GetComponent<SwordComboController>();
            if (hitFeel == null) hitFeel = GetComponent<HitFeelController>();
        }

        private void OnEnable()
        {
            if (combo == null) return;
            combo.OnSwingStart += HandleSwingStart;
            combo.OnImpact += HandleImpact;
            _subscribed = combo;
        }

        private void OnDisable()
        {
            if (_subscribed == null) return;
            _subscribed.OnSwingStart -= HandleSwingStart;
            _subscribed.OnImpact -= HandleImpact;
            _subscribed = null;
        }

        private void OnDestroy()
        {
            if (_scaled == null) return;
            for (int i = 0; i < _scaled.Length; i++)
                if (_scaled[i] != null) Destroy(_scaled[i]);
        }

        private void HandleSwingStart(int step)
        {
            if (!lungeOnSwing || hitFeel == null) return;
            HitFeelProfile p = GetScaled(step);
            if (p != null) hitFeel.PlaySwingStart(p);
        }

        private void HandleImpact(HitInfo hit, int step)
        {
            HitFeelProfile p = GetScaled(step);
            if (p == null) return;

            // Эффекты HitFeel: остановка анимации цели, отброс, искры, тряска, звук.
            if (hitFeel != null) hitFeel.PlayHit(p, hit, hitFeel.weaponAnimator);

            // Остановка кадра для самого замаха — через локальную скорость, не Time.timeScale.
            if (freezeSwingOnHit && combo != null) combo.FreezeSwing(p.hitStopSeconds, swingSpeedDuringStop);
        }

        private HitFeelProfile GetScaled(int step)
        {
            HitFeelProfile src = baseProfile != null ? baseProfile : (hitFeel != null ? hitFeel.profile : null);
            if (src == null || combo == null) return null;

            int count = Mathf.Max(1, combo.SwingCount);
            if (_scaled == null || _scaled.Length != count || _scaledSource != src)
            {
                OnDestroy();
                _scaled = new HitFeelProfile[count];
                _scaledSource = src;
            }
            step = Mathf.Clamp(step, 0, count - 1);
            if (_scaled[step] == null)
            {
                _scaled[step] = Instantiate(src);
                _scaled[step].hideFlags = HideFlags.DontSave;
            }

            HitFeelProfile p = _scaled[step];
            SwingProfile s = combo.GetSwing(step);
            float hs = s != null ? s.hitStopMultiplier : 1f;
            float kb = s != null ? s.knockbackMultiplier : 1f;
            float sh = s != null ? s.shakeMultiplier : 1f;
            float lg = s != null ? s.lungeMultiplier : 1f;

            // Каждый раз берём свежие значения из базового профиля и умножаем.
            p.hitStopSeconds = src.hitStopSeconds * hs;
            p.knockbackDistance = src.knockbackDistance * kb;
            p.knockbackUp = src.knockbackUp * kb;
            p.shakePosAmplitude = src.shakePosAmplitude * sh;
            p.shakeRollAmplitudeDeg = src.shakeRollAmplitudeDeg * sh;
            p.shakeRecoilBack = src.shakeRecoilBack * sh;
            p.lungeDistance = src.lungeDistance * lg;
            // Остальные поля копируем как есть (на случай правки базового профиля в Play Mode).
            p.attackerAnimSpeedDuringStop = src.attackerAnimSpeedDuringStop;
            p.targetAnimSpeedDuringStop = src.targetAnimSpeedDuringStop;
            p.knockbackDuration = src.knockbackDuration;
            p.sparksPrefab = src.sparksPrefab;
            p.sparksLifetime = src.sparksLifetime;
            p.shakeDuration = src.shakeDuration;
            p.shakeFrequency = src.shakeFrequency;
            p.lungeDuration = src.lungeDuration;
            p.lungeDamping = src.lungeDamping;
            p.hitSound = src.hitSound;
            p.audioVolume = src.audioVolume;
            return p;
        }
    }
}
