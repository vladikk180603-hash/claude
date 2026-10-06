using UnityEngine;
using EggGame.HitFeel;

namespace EggGame.SwordCombo
{
    // ЗАГЛУШКА. Если в проекте уже есть свой интерфейс урона — используйте его
    // в LocalDamageSender (или своём отправителе), а этот можно не реализовывать.
    public interface IDamageable
    {
        // Нанести урон. Должно вызываться ТОЛЬКО на хосте.
        void ApplyDamage(float amount, Vector3 point, Vector3 direction, GameObject attacker);
    }

    // Запрос «я ударил» от клиента. Само решение об уроне принимает тот, кто его получит.
    public struct DamageRequest
    {
        public GameObject attacker;
        public GameObject target;      // корень цели (для поиска сетевого объекта)
        public Collider targetCollider;
        public Vector3 point;
        public Vector3 direction;
        public int comboStep;          // 0, 1, 2
        public float damageMultiplier; // из SwingProfile
    }

    // Куда отправить запрос урона. Сейчас — LocalDamageSender (без сети).
    // Потом: свой класс, который отправляет RPC хосту в Photon Fusion 2.
    public interface IDamageRequestSender
    {
        void SendDamageRequest(DamageRequest request);
    }

    // Утилита: собрать запрос из HitInfo.
    public static class DamageRequestUtil
    {
        public static DamageRequest FromHit(HitInfo hit, int step, float damageMultiplier, GameObject targetRoot)
        {
            DamageRequest r;
            r.attacker = hit.attacker != null ? hit.attacker.gameObject : null;
            r.target = targetRoot;
            r.targetCollider = hit.target;
            r.point = hit.contactPoint;
            r.direction = hit.direction;
            r.comboStep = step;
            r.damageMultiplier = damageMultiplier;
            return r;
        }
    }
}
