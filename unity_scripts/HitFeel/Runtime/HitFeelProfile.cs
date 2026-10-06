using UnityEngine;

namespace EggGame.HitFeel
{
    // Набор настроек «сочности» удара для одного типа оружия.
    [CreateAssetMenu(fileName = "HitFeelProfile", menuName = "Egg Game/Hit Feel Profile")]
    public class HitFeelProfile : ScriptableObject
    {
        [Header("Hit Stop (замирание анимации)")]
        [Tooltip("Сколько секунд анимация оружия и цели «замирает» при попадании. 0.04–0.1 — нормально.")]
        [Min(0f)] public float hitStopSeconds = 0.06f;

        [Tooltip("Скорость Animator оружия/рук во время замирания. 0 = полная остановка.")]
        [Range(0f, 1f)] public float attackerAnimSpeedDuringStop = 0f;

        [Tooltip("Скорость Animator цели во время замирания. Маленькое значение = цель почти застывает.")]
        [Range(0f, 1f)] public float targetAnimSpeedDuringStop = 0.05f;

        [Header("Knockback (отталкивание цели)")]
        [Tooltip("На сколько метров отлетает цель.")]
        [Min(0f)] public float knockbackDistance = 1.2f;

        [Tooltip("За сколько секунд цель пролетает это расстояние.")]
        [Min(0.01f)] public float knockbackDuration = 0.2f;

        [Tooltip("Подброс вверх в метрах (для врагов на NavMeshAgent игнорируется).")]
        [Min(0f)] public float knockbackUp = 0f;

        [Header("Искры")]
        [Tooltip("Префаб эффекта искр в точке попадания (лучше с ParticleSystem). Можно оставить пустым.")]
        public GameObject sparksPrefab;

        [Tooltip("Через сколько секунд эффект искр возвращается в пул.")]
        [Min(0.05f)] public float sparksLifetime = 0.6f;

        [Header("Тряска камеры")]
        [Tooltip("Сила дрожания позиции камеры в метрах.")]
        [Min(0f)] public float shakePosAmplitude = 0.03f;

        [Tooltip("Сила наклона (крена) камеры в градусах.")]
        [Min(0f)] public float shakeRollAmplitudeDeg = 1.2f;

        [Tooltip("Отдача камеры назад (по оси вперёд-назад) в метрах.")]
        [Min(0f)] public float shakeRecoilBack = 0.06f;

        [Tooltip("Сколько секунд длится тряска.")]
        [Min(0.01f)] public float shakeDuration = 0.18f;

        [Tooltip("Частота дрожания (чем больше, тем «мельче и быстрее»).")]
        [Min(0.1f)] public float shakeFrequency = 28f;

        [Header("Рывок вперёд при замахе")]
        [Tooltip("На сколько метров игрок подаётся вперёд при замахе.")]
        [Min(0f)] public float lungeDistance = 0.8f;

        [Tooltip("Сколько секунд длится рывок.")]
        [Min(0.01f)] public float lungeDuration = 0.18f;

        [Tooltip("Затухание рывка: больше = резкий старт и быстрая остановка.")]
        [Min(0.01f)] public float lungeDamping = 8f;

        [Header("Звук")]
        [Tooltip("Звук попадания (необязательно).")]
        public AudioClip hitSound;

        [Tooltip("Громкость звука попадания.")]
        [Range(0f, 1f)] public float audioVolume = 1f;
    }
}
