using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EggGame.HurtFeedback
{
    // Проверка без врагов: F5 — 10 урона слева, F6 — справа, F7 — сзади, F8 — 30 урона спереди (сильный), F9 — вылечить.
    public class PlayerHurtDebug : MonoBehaviour
    {
        public PlayerHurtFeedback feedback;
        [Tooltip("Если есть заглушка здоровья — урон идёт через неё (как в игре).")]
        public PlayerHealthStub healthStub;
        [Tooltip("Расстояние до «врага».")]
        [Min(0.5f)] public float sourceDistance = 2f;

        private void Awake()
        {
            if (feedback == null) feedback = GetComponent<PlayerHurtFeedback>();
            if (feedback == null) feedback = FindFirstObjectByType<PlayerHurtFeedback>();
            if (healthStub == null) healthStub = GetComponent<PlayerHealthStub>();
        }

        private void Update()
        {
            if (Pressed(0)) Hit(10f, -90f);
            if (Pressed(1)) Hit(10f, 90f);
            if (Pressed(2)) Hit(10f, 180f);
            if (Pressed(3)) Hit(30f, 0f);
            if (Pressed(4)) HealFull();
        }

        // angle: 0 спереди, 90 справа, -90 слева, 180 сзади (относительно взгляда камеры).
        public void Hit(float damage, float angle)
        {
            if (feedback == null) return;
            Camera c = feedback.playerCamera != null ? feedback.playerCamera : Camera.main;
            Transform t = c != null ? c.transform : feedback.transform;
            Vector3 fwd = t.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * fwd.normalized;
            Vector3 src = t.position + dir * sourceDistance;

            if (healthStub != null) healthStub.ApplyDamage(damage, src);
            else feedback.OnPlayerDamaged(damage, feedback.MaxHealth, src);
        }

        public void HealFull()
        {
            if (healthStub != null) healthStub.Heal(healthStub.maxHealth);
            else if (feedback != null) feedback.SetHealth(feedback.MaxHealth, feedback.MaxHealth);
        }

        // 0=F5, 1=F6, 2=F7, 3=F8, 4=F9
        private static bool Pressed(int key)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            switch (key)
            {
                case 0: return kb.f5Key.wasPressedThisFrame;
                case 1: return kb.f6Key.wasPressedThisFrame;
                case 2: return kb.f7Key.wasPressedThisFrame;
                case 3: return kb.f8Key.wasPressedThisFrame;
                default: return kb.f9Key.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (key)
            {
                case 0: return Input.GetKeyDown(KeyCode.F5);
                case 1: return Input.GetKeyDown(KeyCode.F6);
                case 2: return Input.GetKeyDown(KeyCode.F7);
                case 3: return Input.GetKeyDown(KeyCode.F8);
                default: return Input.GetKeyDown(KeyCode.F9);
            }
#else
            return false;
#endif
        }
    }
}
