using UnityEngine;

namespace EggGame.HitFeel
{
    // Данные об одном попадании.
    [System.Serializable]
    public struct HitInfo
    {
        [Tooltip("Кто ударил (обычно корень игрока).")]
        public Transform attacker;

        [Tooltip("Коллайдер, по которому попали.")]
        public Collider target;

        [Tooltip("Точка контакта в мировых координатах.")]
        public Vector3 contactPoint;

        [Tooltip("Нормаль поверхности цели в точке контакта (смотрит на оружие).")]
        public Vector3 hitNormal;

        [Tooltip("Направление удара от атакующего к цели (нормализовано).")]
        public Vector3 direction;

        public HitInfo(Transform attacker, Collider target, Vector3 contactPoint, Vector3 hitNormal, Vector3 direction)
        {
            this.attacker = attacker;
            this.target = target;
            this.contactPoint = contactPoint;
            this.hitNormal = hitNormal;
            this.direction = direction;
        }
    }
}
