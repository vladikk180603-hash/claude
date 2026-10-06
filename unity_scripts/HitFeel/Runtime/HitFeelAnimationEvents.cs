using UnityEngine;

namespace EggGame.HitFeel
{
    // Animation Events вызывают методы только на объекте, где стоит Animator.
    // Этот компонент ставится рядом с Animator рук/оружия и передаёт события мечу и контроллеру.
    public class HitFeelAnimationEvents : MonoBehaviour
    {
        [Tooltip("Хитбокс меча, которому передаются события.")]
        public MeleeHitbox hitbox;

        [Tooltip("Контроллер для SwingStart (рывок). Если пусто — ищется в родителях.")]
        public HitFeelController controller;

        private void Awake()
        {
            if (hitbox == null) hitbox = GetComponentInChildren<MeleeHitbox>(true);
            if (controller == null) controller = GetComponentInParent<HitFeelController>();
        }

        public void HitboxOn() { if (hitbox != null) hitbox.HitboxOn(); }
        public void HitboxOff() { if (hitbox != null) hitbox.HitboxOff(); }
        public void TrailOn() { if (hitbox != null) hitbox.TrailOn(); }
        public void TrailOff() { if (hitbox != null) hitbox.TrailOff(); }
        public void SwingStart() { if (controller != null) controller.PlaySwingStart(); }
    }
}
