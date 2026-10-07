using UnityEngine;

namespace EggGame.HurtFeedback
{
    // Точка входа для кода здоровья игрока (или для Fusion RPC от хоста).
    // Реализует PlayerHurtFeedback. Всё — только показ: урон и неуязвимость решает хост.
    public interface IPlayerDamageFeedback
    {
        // Игроку нанесли урон. sourceWorldPosition — где стоит тот, кто ударил.
        void OnPlayerDamaged(float damage, float maxHealth, Vector3 sourceWorldPosition);

        // Актуальное здоровье (после урона, лечения, возрождения).
        void SetHealth(float currentHealth, float maxHealth);

        // Показать неуязвимость на столько секунд (хост решил, что игрок неуязвим).
        void ShowInvulnerability(float seconds);
    }
}
