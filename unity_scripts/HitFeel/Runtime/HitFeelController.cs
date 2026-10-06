using System;
using UnityEngine;

namespace EggGame.HitFeel
{
    // Собирает весь «сок» удара. Ставится на игрока. Работает только локально.
    public class HitFeelController : MonoBehaviour
    {
        [Tooltip("Профиль по умолчанию (используется при автоподписке на хитбокс).")]
        public HitFeelProfile profile;

        [Tooltip("Хитбокс меча. Если задан — контроллер сам подписывается на его попадания.")]
        public MeleeHitbox hitbox;

        [Tooltip("Animator оружия/рук, который замирает при попадании.")]
        public Animator weaponAnimator;

        [Tooltip("Рывок при замахе. Если пусто — ищется на этом объекте.")]
        public LungeMotion lunge;

        [Tooltip("Источник звука (необязательно). Если пусто — звук играется в точке удара.")]
        public AudioSource audioSource;

        [Tooltip("Это мой (локальный) игрок? Для чужих игроков выключите — эффекты не будут играть.")]
        public bool isLocalPlayer = true;

        [Tooltip("Замедлять ли Animator цели во время Hit Stop.")]
        public bool stopTargetAnimator = true;

        [Tooltip("Запускать рывок автоматически, когда хитбокс открывается (HitboxOn).")]
        public bool lungeOnHitboxOpen = false;

        // Вызывается после проигрывания эффектов (для звуковой системы и т.п.).
        public event Action<HitInfo, HitFeelProfile> OnHitFeelPlayed;
        // Вызывается при начале замаха.
        public event Action<HitFeelProfile> OnSwingStarted;

        // Кеш массивов, чтобы не создавать их на каждый удар.
        private readonly Animator[] _stopAnimators = new Animator[2];
        private readonly float[] _stopSpeeds = new float[2];
        private MeleeHitbox _subscribed;

        private void Awake()
        {
            if (lunge == null) lunge = GetComponent<LungeMotion>();
        }

        private void OnEnable() { Subscribe(); }
        private void OnDisable() { Unsubscribe(); }

        // Сменить хитбокс во время игры (например, при смене оружия).
        public void SetHitbox(MeleeHitbox newHitbox)
        {
            Unsubscribe();
            hitbox = newHitbox;
            if (isActiveAndEnabled) Subscribe();
        }

        private void Subscribe()
        {
            if (hitbox == null || _subscribed == hitbox) return;
            Unsubscribe();
            hitbox.OnMeleeHit += HandleMeleeHit;
            hitbox.OnHitboxOpened += HandleHitboxOpened;
            _subscribed = hitbox;
        }

        private void Unsubscribe()
        {
            if (_subscribed == null) return;
            _subscribed.OnMeleeHit -= HandleMeleeHit;
            _subscribed.OnHitboxOpened -= HandleHitboxOpened;
            _subscribed = null;
        }

        private void HandleMeleeHit(HitInfo info) { PlayHit(profile, info, weaponAnimator); }
        private void HandleHitboxOpened() { if (lungeOnHitboxOpen) PlaySwingStart(profile); }

        // Всё в одном кадре: HitStop, Knockback, искры, тряска, звук, событие.
        public void PlayHit(HitFeelProfile p, HitInfo info, Animator weaponAnim)
        {
            if (p == null || !isLocalPlayer) return;

            // 1. Hit Stop
            Animator targetAnim = null;
            if (stopTargetAnimator && info.target != null)
                targetAnim = info.target.GetComponentInParent<Animator>();
            if (targetAnim == weaponAnim) targetAnim = null;
            _stopAnimators[0] = weaponAnim;
            _stopSpeeds[0] = p.attackerAnimSpeedDuringStop;
            _stopAnimators[1] = targetAnim;
            _stopSpeeds[1] = p.targetAnimSpeedDuringStop;
            HitStopService.Request(_stopAnimators, _stopSpeeds, p.hitStopSeconds);
            _stopAnimators[0] = null;
            _stopAnimators[1] = null;

            // 2. Knockback
            if (info.target != null)
            {
                KnockbackReceiver kr = info.target.GetComponentInParent<KnockbackReceiver>();
                if (kr != null) kr.Apply(info.direction, p.knockbackDistance, p.knockbackDuration, p.knockbackUp);
            }

            // 3. Искры
            if (p.sparksPrefab != null)
            {
                HitSparkSpawner sp = HitSparkSpawner.Instance;
                if (sp != null) sp.Spawn(p.sparksPrefab, info.contactPoint, info.hitNormal, p.sparksLifetime);
            }

            // 4. Тряска камеры
            CameraShake.ImpulseLocal(p, info.direction);

            // 5. Звук
            if (p.hitSound != null)
            {
                if (audioSource != null) audioSource.PlayOneShot(p.hitSound, p.audioVolume);
                else AudioSource.PlayClipAtPoint(p.hitSound, info.contactPoint, p.audioVolume);
            }

            // 6. Событие
            if (OnHitFeelPlayed != null) OnHitFeelPlayed(info, p);
        }

        // Начало замаха: рывок вперёд по взгляду (горизонтально).
        public void PlaySwingStart(HitFeelProfile p)
        {
            if (p == null || !isLocalPlayer) return;
            if (lunge != null)
            {
                Vector3 fwd = GetForward();
                lunge.Begin(fwd, p.lungeDistance, p.lungeDuration, p.lungeDamping);
            }
            if (OnSwingStarted != null) OnSwingStarted(p);
        }

        // Вариант без параметров — для Animation Event на клипе взмаха.
        public void PlaySwingStart() { PlaySwingStart(profile); }

        private Vector3 GetForward()
        {
            // От первого лица направление взгляда даёт камера.
            CameraShake cs = CameraShake.Local;
            Transform t = cs != null ? cs.transform : transform;
            Vector3 f = t.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-6f) f = transform.forward;
            return f;
        }
    }
}
