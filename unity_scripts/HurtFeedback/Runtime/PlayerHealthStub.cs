using UnityEngine;

namespace EggGame.HurtFeedback
{
    // ЗАГЛУШКА здоровья игрока — показывает, КАК подключить эффекты к коду здоровья.
    // Если в проекте уже есть здоровье игрока, этот компонент не нужен: в нём добавьте
    // те же 2–3 вызова (см. «>>> ПОДКЛЮЧЕНИЕ <<<» ниже).
    public class PlayerHealthStub : MonoBehaviour
    {
        [Min(1f)] public float maxHealth = 100f;
        public float health = 100f;
        [Tooltip("Неуязвимость после удара (в сети это решает ХОСТ).")]
        [Min(0f)] public float invulnerabilitySeconds = 0.5f;

        public PlayerHurtFeedback feedback;
        private float _invulnerableUntil;

        private void Awake()
        {
            if (feedback == null) feedback = GetComponent<PlayerHurtFeedback>();
            health = Mathf.Clamp(health, 0f, maxHealth);
        }

        private void Start()
        {
            if (feedback != null) feedback.SetHealth(health, maxHealth);
        }

        // В сети этот метод выполняет ХОСТ, а клиенту пострадавшего уходит RPC с (damage, maxHealth, sourcePos).
        public void ApplyDamage(float damage, Vector3 sourceWorldPosition)
        {
            if (damage <= 0f || health <= 0f) return;
            if (Time.time < _invulnerableUntil) return; // неуязвимость: решение хоста
            _invulnerableUntil = Time.time + invulnerabilitySeconds;
            health = Mathf.Max(0f, health - damage);

            // >>> ПОДКЛЮЧЕНИЕ <<< — эти вызовы нужны в настоящем коде здоровья (на клиенте пострадавшего):
            if (feedback != null)
            {
                feedback.SetHealth(health, maxHealth);
                feedback.OnPlayerDamaged(damage, maxHealth, sourceWorldPosition);
                // feedback.ShowInvulnerability(invulnerabilitySeconds); // если в конфиге выключено showInvulnerabilityOnDamage
            }
        }

        public void Heal(float amount)
        {
            health = Mathf.Min(maxHealth, health + Mathf.Max(0f, amount));
            if (feedback != null) feedback.SetHealth(health, maxHealth);
        }
    }
}
