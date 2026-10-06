using UnityEngine;

namespace EggGame.SwordCombo
{
    // Простой отправитель урона БЕЗ сети: сразу ищет IDamageable на цели и наносит урон.
    // Для мультиплеера замените на свой компонент с IDamageRequestSender, который шлёт RPC хосту.
    public class LocalDamageSender : MonoBehaviour, IDamageRequestSender
    {
        [Tooltip("Базовый урон меча. Итог = базовый × множитель удара из SwingProfile.")]
        [Min(0f)] public float baseDamage = 10f;

        [Tooltip("Писать попадания в Console (для отладки).")]
        public bool logHits = true;

        public void SendDamageRequest(DamageRequest request)
        {
            if (request.targetCollider == null) return;
            float amount = baseDamage * request.damageMultiplier;

            // >>> МЕСТО ПОДКЛЮЧЕНИЯ СУЩЕСТВУЮЩЕЙ СИСТЕМЫ УРОНА <<<
            // Если в проекте свой интерфейс урона, замените поиск IDamageable ниже на него.
            IDamageable target = request.targetCollider.GetComponentInParent<IDamageable>();
            if (target != null)
                target.ApplyDamage(amount, request.point, request.direction, request.attacker);

            if (logHits)
            {
                string who = request.target != null ? request.target.name : request.targetCollider.name;
                Debug.Log("[SwordCombo] Удар " + (request.comboStep + 1) + " по " + who + ", урон " + amount +
                          (target == null ? " (на цели нет IDamageable — урон не нанесён)" : ""), this);
            }
        }
    }
}
