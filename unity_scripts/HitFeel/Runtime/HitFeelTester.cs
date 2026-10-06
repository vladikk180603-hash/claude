using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EggGame.HitFeel
{
    // Проверка без анимаций: H — удар, J — рывок, K — тряска.
    public class HitFeelTester : MonoBehaviour
    {
        public HitFeelController controller;
        public HitFeelProfile profile;

        [Tooltip("Камера для рейкаста. Если пусто — Camera.main.")]
        public Camera cam;

        [Tooltip("Дальность «удара» в метрах.")]
        [Min(0.1f)] public float range = 3f;

        public LayerMask hitLayers = ~0;

        [Tooltip("Animator оружия для Hit Stop (необязательно).")]
        public Animator weaponAnimator;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<HitFeelController>();
            if (controller == null) controller = FindFirstObjectByType<HitFeelController>();
        }

        private void Update()
        {
            if (KeyDown(0)) SimulateHit();
            if (KeyDown(1)) SimulateSwing();
            if (KeyDown(2)) SimulateShake();
        }

        // 0 = H, 1 = J, 2 = K
        private static bool KeyDown(int key)
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            switch (key)
            {
                case 0: return kb.hKey.wasPressedThisFrame;
                case 1: return kb.jKey.wasPressedThisFrame;
                default: return kb.kKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (key)
            {
                case 0: return Input.GetKeyDown(KeyCode.H);
                case 1: return Input.GetKeyDown(KeyCode.J);
                default: return Input.GetKeyDown(KeyCode.K);
            }
#else
            return false;
#endif
        }

        private HitFeelProfile Profile()
        {
            if (profile != null) return profile;
            return controller != null ? controller.profile : null;
        }

        private Camera Cam()
        {
            return cam != null ? cam : Camera.main;
        }

        public void SimulateHit()
        {
            Camera c = Cam();
            HitFeelProfile p = Profile();
            if (c == null || controller == null || p == null)
            {
                Debug.LogWarning("[HitFeel] Tester: нужны камера, HitFeelController и профиль.", this);
                return;
            }
            Ray ray = new Ray(c.transform.position, c.transform.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, range, hitLayers, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("[HitFeel] Tester: перед камерой ничего нет.", this);
                return;
            }
            Vector3 dir = c.transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = controller.transform.forward;
            dir.Normalize();

            HitInfo info = new HitInfo(controller.transform, hit.collider, hit.point, hit.normal, dir);
            Animator wa = weaponAnimator != null ? weaponAnimator : controller.weaponAnimator;
            controller.PlayHit(p, info, wa);
        }

        public void SimulateSwing()
        {
            HitFeelProfile p = Profile();
            if (controller != null && p != null) controller.PlaySwingStart(p);
        }

        public void SimulateShake()
        {
            HitFeelProfile p = Profile();
            Camera c = Cam();
            if (p == null) return;
            CameraShake.ImpulseLocal(p, c != null ? c.transform.forward : Vector3.forward);
        }
    }
}
