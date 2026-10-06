using UnityEngine;

namespace EggGame.SwordCombo
{
    // Манекен для проверки ударов: принимает урон через заглушку IDamageable и пишет в Console.
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        public float health = 100f;

        public void ApplyDamage(float amount, Vector3 point, Vector3 direction, GameObject attacker)
        {
            health -= amount;
            Debug.Log("[Dummy] " + name + " получил " + amount + ", осталось " + health, this);
            if (health <= 0f) health = 100f; // манекен бессмертный
        }
    }
}
