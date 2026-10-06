using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace EggGame.HitFeel
{
    [Serializable]
    public class HitInfoEvent : UnityEvent<HitInfo> { }

    // Хитбокс меча. Ставится на меч рядом с коллайдером-триггером.
    // Урон НЕ наносит — только сообщает о попадании (OnMeleeHit).
    public class MeleeHitbox : MonoBehaviour
    {
        [Header("Хитбокс")]
        [Tooltip("Коллайдер-триггер меча. Если пусто — берётся с этого объекта.")]
        public Collider hitboxCollider;

        [Tooltip("Кто атакует (корень игрока). Если пусто — корень иерархии.")]
        public Transform attacker;

        [Tooltip("Точка на клинке, от которой считается контакт (необязательно). Если пусто — центр коллайдера.")]
        public Transform contactReference;

        [Tooltip("По каким слоям можно попадать.")]
        public LayerMask targetLayers = ~0;

        [Tooltip("Игнорировать коллайдеры-триггеры у целей.")]
        public bool ignoreTriggerTargets = true;

        [Tooltip("Добавить кинематический Rigidbody на меч, если его нет (нужен, чтобы срабатывал OnTriggerEnter).")]
        public bool autoAddKinematicRigidbody = true;

        [Header("Шлейфы (TrailOn/TrailOff)")]
        public TrailRenderer[] trailRenderers = new TrailRenderer[0];
        public ParticleSystem[] trailParticles = new ParticleSystem[0];
        public GameObject[] trailObjects = new GameObject[0];

        [Header("События")]
        [Tooltip("Вызывается при попадании (для настройки в инспекторе).")]
        public HitInfoEvent onMeleeHitUnity = new HitInfoEvent();

        // C#-событие попадания: сюда подписывается система урона.
        public event Action<HitInfo> OnMeleeHit;
        // Окно удара открылось/закрылось.
        public event Action OnHitboxOpened;
        public event Action OnHitboxClosed;

        public bool IsHitboxActive { get; private set; }

        private readonly HashSet<UnityEngine.Object> _hitThisSwing = new HashSet<UnityEngine.Object>();
        private Coroutine _swingRoutine;

        private void Awake()
        {
            if (hitboxCollider == null) hitboxCollider = GetComponent<Collider>();
            if (attacker == null) attacker = transform.root;
            if (hitboxCollider != null)
            {
                hitboxCollider.isTrigger = true;
                hitboxCollider.enabled = false;
                if (autoAddKinematicRigidbody && hitboxCollider.attachedRigidbody == null)
                {
                    Rigidbody rb = hitboxCollider.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
            else
            {
                Debug.LogWarning("[HitFeel] MeleeHitbox: не найден Collider на " + name, this);
            }
        }

        // ---- Методы для Animation Events ----

        public void HitboxOn()
        {
            _hitThisSwing.Clear();
            IsHitboxActive = true;
            if (hitboxCollider != null) hitboxCollider.enabled = true;
            if (OnHitboxOpened != null) OnHitboxOpened();
        }

        public void HitboxOff()
        {
            bool was = IsHitboxActive;
            IsHitboxActive = false;
            if (hitboxCollider != null) hitboxCollider.enabled = false;
            if (was && OnHitboxClosed != null) OnHitboxClosed();
        }

        public void TrailOn()
        {
            for (int i = 0; i < trailRenderers.Length; i++)
            {
                if (trailRenderers[i] == null) continue;
                trailRenderers[i].Clear();
                trailRenderers[i].emitting = true;
            }
            for (int i = 0; i < trailParticles.Length; i++)
                if (trailParticles[i] != null) trailParticles[i].Play(true);
            for (int i = 0; i < trailObjects.Length; i++)
                if (trailObjects[i] != null) trailObjects[i].SetActive(true);
        }

        public void TrailOff()
        {
            for (int i = 0; i < trailRenderers.Length; i++)
                if (trailRenderers[i] != null) trailRenderers[i].emitting = false;
            for (int i = 0; i < trailParticles.Length; i++)
                if (trailParticles[i] != null) trailParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            for (int i = 0; i < trailObjects.Length; i++)
                if (trailObjects[i] != null) trailObjects[i].SetActive(false);
        }

        // Окно удара по таймеру — пока нет анимации с событиями.
        public void StartSwingWithoutAnimation(float activeDelay, float activeDuration)
        {
            if (!isActiveAndEnabled) return;
            if (_swingRoutine != null) StopCoroutine(_swingRoutine);
            _swingRoutine = StartCoroutine(SwingRoutine(Mathf.Max(0f, activeDelay), Mathf.Max(0.01f, activeDuration)));
        }

        private IEnumerator SwingRoutine(float delay, float duration)
        {
            TrailOn();
            if (delay > 0f) yield return new WaitForSeconds(delay);
            HitboxOn();
            yield return new WaitForSeconds(duration);
            HitboxOff();
            TrailOff();
            _swingRoutine = null;
        }

        private void OnDisable()
        {
            if (_swingRoutine != null) { StopCoroutine(_swingRoutine); _swingRoutine = null; }
            HitboxOff();
            TrailOff();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsHitboxActive || other == null) return;
            if (ignoreTriggerTargets && other.isTrigger) return;
            if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
            if (attacker != null && other.transform.IsChildOf(attacker)) return; // себя не бьём

            // Одна цель = один раз за замах (даже если у неё несколько коллайдеров).
            UnityEngine.Object key = GetTargetKey(other);
            if (!_hitThisSwing.Add(key)) return;

            Vector3 weaponPos = contactReference != null
                ? contactReference.position
                : (hitboxCollider != null ? hitboxCollider.bounds.center : transform.position);

            Vector3 contact = SafeClosestPoint(other, weaponPos);

            Vector3 attackerPos = attacker != null ? attacker.position : weaponPos;
            Vector3 dir = contact - attackerPos;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
                dir = attacker != null ? attacker.forward : transform.forward;
            dir.Normalize();

            Vector3 normal = weaponPos - contact;
            normal = normal.sqrMagnitude > 1e-6f ? normal.normalized : -dir;

            HitInfo info = new HitInfo(attacker, other, contact, normal, dir);
            if (OnMeleeHit != null) OnMeleeHit(info);
            if (onMeleeHitUnity != null) onMeleeHitUnity.Invoke(info);
        }

        private static UnityEngine.Object GetTargetKey(Collider c)
        {
            KnockbackReceiver kr = c.GetComponentInParent<KnockbackReceiver>();
            if (kr != null) return kr.gameObject;
            if (c.attachedRigidbody != null) return c.attachedRigidbody.gameObject;
            return c;
        }

        // ClosestPoint работает только с Box/Sphere/Capsule/выпуклым Mesh.
        private static Vector3 SafeClosestPoint(Collider c, Vector3 from)
        {
            MeshCollider mc = c as MeshCollider;
            if (mc != null && !mc.convex) return c.bounds.ClosestPoint(from);
            Vector3 p = c.ClosestPoint(from);
            if (p == from) p = c.bounds.ClosestPoint(from); // точка внутри коллайдера
            if (p == from) p = c.bounds.center;
            return p;
        }
    }
}
