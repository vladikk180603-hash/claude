using UnityEngine;
using UnityEngine.Audio;

namespace EggGame.HurtFeedback
{
    // Все настройки эффектов «меня ударили». Один ассет на игру.
    [CreateAssetMenu(fileName = "PlayerHurtFeedbackConfig", menuName = "Egg Game/Player Hurt Feedback Config")]
    public class PlayerHurtFeedbackConfig : ScriptableObject
    {
        [Header("Сила удара → интенсивность эффектов")]
        [Tooltip("intensity = clamp(урон / макс.здоровье × K, Min, 1). Больше K — сильнее эффекты от слабых ударов.")]
        [Min(0f)] public float intensityK = 4f;
        [Tooltip("Минимальная интенсивность даже для самого слабого удара (чтобы его всегда было заметно).")]
        [Range(0f, 1f)] public float minIntensity = 0.4f;
        [Tooltip("Ниже этой доли здоровья — режим «мало здоровья» (пульс, приглушение звука, сердце).")]
        [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;
        [Tooltip("Макс. здоровье, если код здоровья его не передал.")]
        [Min(1f)] public float defaultMaxHealth = 100f;
        [Tooltip("Тряска, наклон и отброс — не чаще, чем раз в столько секунд (защита от хаоса).")]
        [Min(0f)] public float effectMinInterval = 0.15f;

        [Header("a) Виньетка")]
        [Range(0f, 1f)] public float vignetteMultiplier = 1f;
        public Color vignetteColor = new Color(0.75f, 0f, 0f, 1f);
        [Tooltip("Яркость виньетки при интенсивности 1.")]
        [Range(0f, 1f)] public float vignetteMaxAlpha = 0.7f;
        [Tooltip("Сколько секунд нарастает вспышка.")]
        [Min(0.001f)] public float vignetteAttack = 0.05f;
        [Tooltip("Сколько секунд затухает.")]
        [Min(0.01f)] public float vignetteFade = 0.4f;
        [Tooltip("Потолок яркости при серии ударов (вспышки складываются до него).")]
        [Range(0f, 1f)] public float vignetteCap = 0.85f;
        [Tooltip("Размер чистого центра экрана (0.3 — узкий, 0.7 — только края).")]
        [Range(0.05f, 0.95f)] public float vignetteInnerRadius = 0.5f;
        [Tooltip("Яркость пульса при малом здоровье.")]
        [Range(0f, 1f)] public float lowHealthPulseAlpha = 0.25f;
        [Tooltip("Частота пульса, раз в секунду.")]
        [Min(0.1f)] public float lowHealthPulseRate = 1f;

        [Header("b) Указатель направления")]
        [Range(0f, 1f)] public float indicatorMultiplier = 1f;
        public Color indicatorColor = new Color(1f, 0.1f, 0.1f, 1f);
        [Tooltip("Сколько секунд живёт дуга.")]
        [Min(0.1f)] public float indicatorLifetime = 1f;
        [Tooltip("Максимум дуг одновременно (новая заменяет самую старую).")]
        [Range(1, 12)] public int indicatorMaxCount = 4;
        [Tooltip("Радиус дуги от центра экрана (пиксели при 1920×1080).")]
        [Min(20f)] public float indicatorRadius = 160f;
        [Tooltip("Толщина дуги (пиксели при 1920×1080).")]
        [Min(2f)] public float indicatorThickness = 18f;
        [Tooltip("Ширина дуги в градусах.")]
        [Range(10f, 180f)] public float indicatorArcDegrees = 70f;
        [Tooltip("Прозрачность дуги от слабого удара (от сильного — 1).")]
        [Range(0f, 1f)] public float indicatorMinAlpha = 0.55f;

        [Header("c) Камера (через CameraShake)")]
        [Range(0f, 1f)] public float cameraMultiplier = 1f;
        [Tooltip("Тряска: амплитуда позиции, метры (при интенсивности 1).")]
        [Min(0f)] public float shakePosAmplitude = 0.04f;
        [Tooltip("Тряска: случайный крен, градусы.")]
        [Min(0f)] public float shakeRollNoiseDeg = 1f;
        [Tooltip("Тряска: отдача назад, метры.")]
        [Min(0f)] public float shakeRecoil = 0.03f;
        [Min(0.01f)] public float shakeDuration = 0.2f;
        [Min(0.1f)] public float shakeFrequency = 25f;
        [Tooltip("Наклон (крен) в сторону удара, градусы.")]
        [Min(0f)] public float tiltRollDeg = 4f;
        [Tooltip("Кивок от удара спереди/сзади, градусы.")]
        [Min(0f)] public float tiltPitchDeg = 2.5f;
        [Tooltip("Сколько длится наклон.")]
        [Min(0.05f)] public float tiltDuration = 0.3f;

        [Header("d) Тело")]
        [Range(0f, 1f)] public float bodyMultiplier = 1f;
        [Tooltip("Отброс игрока от источника удара, метры (при интенсивности 1).")]
        [Min(0f)] public float knockbackDistance = 0.3f;
        [Min(0.01f)] public float knockbackDuration = 0.12f;
        [Tooltip("«Замирание» ввода игрока, секунды (Time.timeScale не трогается).")]
        [Min(0f)] public float freezeSeconds = 0.06f;
        [Tooltip("Множитель ввода во время замирания (0 = полностью).")]
        [Range(0f, 1f)] public float freezeInputMultiplier = 0f;

        [Header("e) Звук")]
        [Range(0f, 1f)] public float audioMultiplier = 1f;
        [Tooltip("Звуки «удар в тело» (берётся случайный).")]
        public AudioClip[] impactClips = new AudioClip[0];
        [Range(0f, 1f)] public float impactVolume = 0.9f;
        [Tooltip("Вскрики (берётся случайный).")]
        public AudioClip[] screamClips = new AudioClip[0];
        [Range(0f, 1f)] public float screamVolume = 0.8f;
        [Tooltip("Вскрик не чаще, чем раз в столько секунд.")]
        [Min(0f)] public float screamCooldown = 0.8f;
        [Tooltip("Стук сердца при малом здоровье (зацикленный).")]
        public AudioClip heartbeatClip;
        [Range(0f, 1f)] public float heartbeatVolume = 0.6f;
        [Tooltip("Частота среза при малом здоровье (Гц). Меньше — глуше.")]
        [Range(200f, 22000f)] public float lowHealthLowPassHz = 1200f;
        [Tooltip("Необязательно: AudioMixer с открытым параметром частоты Lowpass. Если пусто — фильтр на AudioListener.")]
        public AudioMixer mixer;
        [Tooltip("Имя открытого (Exposed) параметра частоты в миксере.")]
        public string mixerLowPassParam = "LowPassCutoff";

        [Header("f) Неуязвимость (решает хост, тут только показ)")]
        [Min(0f)] public float invulnerabilitySeconds = 0.5f;
        [Tooltip("Показывать неуязвимость сразу при уроне (иначе — только по вызову ShowInvulnerability).")]
        public bool showInvulnerabilityOnDamage = true;
        public Color invulnerabilityTint = new Color(1f, 0.55f, 0.55f, 1f);
        [Tooltip("Миганий в секунду.")]
        [Min(0.5f)] public float invulnerabilityBlinkRate = 10f;

        [Header("g) Полоска здоровья")]
        [Range(0f, 1f)] public float healthBarMultiplier = 1f;
        [Tooltip("Создавать простую полоску здоровья (выключите, если есть своя).")]
        public bool createHealthBar = true;
        public Vector2 healthBarSize = new Vector2(420f, 24f);
        [Tooltip("Отступ от левого нижнего угла.")]
        public Vector2 healthBarOffset = new Vector2(40f, 40f);
        public Color healthBarColor = new Color(0.85f, 0.15f, 0.15f, 1f);
        public Color healthBarLostColor = Color.white;
        public Color healthBarBackground = new Color(0f, 0f, 0f, 0.55f);
        [Tooltip("Сколько пикселей дёргается полоска при сильном ударе.")]
        [Min(0f)] public float healthBarShakePixels = 8f;
        [Min(0.01f)] public float healthBarShakeDuration = 0.25f;
        [Tooltip("Сколько секунд белый потерянный кусок стоит на месте.")]
        [Min(0f)] public float lostChunkHold = 0.3f;
        [Tooltip("За сколько секунд белый кусок съезжает.")]
        [Min(0.01f)] public float lostChunkSlide = 0.5f;

        // Общая формула интенсивности.
        public float ComputeIntensity(float damage, float maxHealth)
        {
            if (maxHealth <= 0f) maxHealth = defaultMaxHealth;
            float raw = damage / maxHealth * intensityK;
            return Mathf.Clamp(raw, minIntensity, 1f);
        }
    }
}
