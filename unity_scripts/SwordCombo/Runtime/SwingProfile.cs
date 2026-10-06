using UnityEngine;

namespace EggGame.SwordCombo
{
    // Один удар серии. Движение меча задаётся кривыми, без анимационных клипов.
    // Кривые — это СМЕЩЕНИЕ от стойки (позиция в метрах, поворот в градусах),
    // ось X кривой — доля времени удара от 0 до 1.
    [CreateAssetMenu(fileName = "Swing", menuName = "Egg Game/Swing Profile")]
    public class SwingProfile : ScriptableObject
    {
        [Tooltip("Название удара (для отладки).")]
        public string displayName = "Удар";

        [Tooltip("Длительность всего замаха в секундах (меч тяжёлый — не меньше 0.5).")]
        [Min(0.1f)] public float duration = 0.55f;

        [Header("Смещение позиции SwordPivot от стойки (метры)")]
        public AnimationCurve posX = AnimationCurve.Constant(0f, 1f, 0f);
        public AnimationCurve posY = AnimationCurve.Constant(0f, 1f, 0f);
        public AnimationCurve posZ = AnimationCurve.Constant(0f, 1f, 0f);

        [Header("Смещение поворота SwordPivot от стойки (градусы)")]
        [Tooltip("Наклон клинка вперёд(+)/назад(-).")]
        public AnimationCurve rotX = AnimationCurve.Constant(0f, 1f, 0f);
        [Tooltip("Поворот клинка влево(-)/вправо(+).")]
        public AnimationCurve rotY = AnimationCurve.Constant(0f, 1f, 0f);
        [Tooltip("Наклон клинка вбок.")]
        public AnimationCurve rotZ = AnimationCurve.Constant(0f, 1f, 0f);

        [Header("Окно удара (доли длительности)")]
        [Range(0f, 1f)] public float hitWindowStart = 0.30f;
        [Range(0f, 1f)] public float hitWindowEnd = 0.55f;

        [Header("Сила")]
        [Tooltip("Множитель урона.")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Tooltip("Множитель остановки кадра (HitFeel).")]
        [Min(0f)] public float hitStopMultiplier = 1f;
        [Tooltip("Множитель отброса врага (HitFeel).")]
        [Min(0f)] public float knockbackMultiplier = 1f;
        [Tooltip("Множитель тряски камеры при попадании (HitFeel).")]
        [Min(0f)] public float shakeMultiplier = 1f;
        [Tooltip("Множитель рывка игрока вперёд при начале удара (HitFeel LungeMotion).")]
        [Min(0f)] public float lungeMultiplier = 1f;

        [Header("Камера во время замаха (добавка в SwingCameraSway)")]
        [Tooltip("Наклон камеры (крен) в градусах на пике замаха. + влево, - вправо.")]
        public float cameraRollDeg = 0f;
        [Tooltip("Кивок камеры в градусах на пике замаха (+ вниз).")]
        public float cameraPitchDeg = 0f;
        [Tooltip("Небольшой сдвиг камеры на пике замаха (метры, локальные оси).")]
        public Vector3 cameraSwayPos = Vector3.zero;

        [Header("Возврат")]
        [Tooltip("Сколько секунд меч возвращается в стойку после удара.")]
        [Min(0.01f)] public float recoveryDuration = 0.3f;

        // Огибающая для камеры: 0 в начале и конце, 1 в середине.
        public static float CameraEnvelope(float t)
        {
            return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        }

        // ---------- Готовые удары (используются меню и как запасной вариант) ----------

        // index 0 = слева направо, 1 = справа налево, 2 = сверху вниз.
        public static SwingProfile CreateDefault(int index)
        {
            SwingProfile p = CreateInstance<SwingProfile>();
            switch (index)
            {
                case 0:
                    p.name = "Swing1_LeftToRight";
                    p.displayName = "Удар 1: слева направо";
                    p.duration = 0.55f;
                    Fill(p,
                        new float[] { 0f, 0.25f, 0.42f, 0.62f, 1f },
                        new float[] { 0f, -0.45f, -0.10f, 0.25f, 0.20f },  // posX
                        new float[] { 0f, 0.18f, 0.12f, 0.08f, 0.02f },    // posY
                        new float[] { 0f, 0.00f, 0.15f, 0.10f, 0.00f },    // posZ
                        new float[] { 0f, 55f, 60f, 60f, 50f },            // rotX
                        new float[] { 0f, -80f, -20f, 55f, 70f },          // rotY
                        new float[] { 0f, -12f, -12f, -12f, -12f });       // rotZ
                    p.hitWindowStart = 0.32f; p.hitWindowEnd = 0.60f;
                    p.damageMultiplier = 1f; p.hitStopMultiplier = 1f; p.knockbackMultiplier = 0.8f;
                    p.shakeMultiplier = 1f; p.lungeMultiplier = 0.8f;
                    p.cameraRollDeg = -2f; p.cameraPitchDeg = 0f; p.cameraSwayPos = new Vector3(0.02f, 0f, 0f);
                    p.recoveryDuration = 0.30f;
                    break;

                case 1:
                    p.name = "Swing2_RightToLeft";
                    p.displayName = "Удар 2: справа налево";
                    p.duration = 0.60f;
                    Fill(p,
                        new float[] { 0f, 0.25f, 0.42f, 0.62f, 1f },
                        new float[] { 0f, 0.25f, 0.00f, -0.35f, -0.30f },
                        new float[] { 0f, 0.10f, 0.06f, 0.02f, 0.00f },
                        new float[] { 0f, -0.05f, 0.15f, 0.10f, 0.00f },
                        new float[] { 0f, 55f, 62f, 62f, 50f },
                        new float[] { 0f, 85f, 25f, -55f, -70f },
                        new float[] { 0f, -30f, -30f, -30f, -25f });
                    p.hitWindowStart = 0.33f; p.hitWindowEnd = 0.60f;
                    p.damageMultiplier = 1.2f; p.hitStopMultiplier = 1.15f; p.knockbackMultiplier = 1f;
                    p.shakeMultiplier = 1.15f; p.lungeMultiplier = 1f;
                    p.cameraRollDeg = 2.5f; p.cameraPitchDeg = 0f; p.cameraSwayPos = new Vector3(-0.025f, 0f, 0f);
                    p.recoveryDuration = 0.32f;
                    break;

                default:
                    p.name = "Swing3_Overhead";
                    p.displayName = "Удар 3: сверху вниз";
                    p.duration = 0.85f;
                    Fill(p,
                        new float[] { 0f, 0.35f, 0.50f, 0.68f, 1f },
                        new float[] { 0f, -0.12f, -0.20f, -0.22f, -0.18f },
                        new float[] { 0f, 0.38f, 0.15f, -0.12f, -0.10f },
                        new float[] { 0f, -0.05f, 0.20f, 0.20f, 0.10f },
                        new float[] { 0f, -50f, 40f, 115f, 105f },
                        new float[] { 0f, -10f, -10f, -8f, -5f },
                        new float[] { 0f, -15f, -15f, -15f, -15f });
                    p.hitWindowStart = 0.45f; p.hitWindowEnd = 0.70f;
                    p.damageMultiplier = 1.8f; p.hitStopMultiplier = 1.6f; p.knockbackMultiplier = 1.7f;
                    p.shakeMultiplier = 1.8f; p.lungeMultiplier = 1.3f;
                    p.cameraRollDeg = 0f; p.cameraPitchDeg = 3f; p.cameraSwayPos = new Vector3(0f, -0.03f, 0.02f);
                    p.recoveryDuration = 0.40f;
                    break;
            }
            return p;
        }

        private static void Fill(SwingProfile p, float[] t, float[] px, float[] py, float[] pz, float[] rx, float[] ry, float[] rz)
        {
            p.posX = SmoothCurve(t, px);
            p.posY = SmoothCurve(t, py);
            p.posZ = SmoothCurve(t, pz);
            p.rotX = SmoothCurve(t, rx);
            p.rotY = SmoothCurve(t, ry);
            p.rotZ = SmoothCurve(t, rz);
        }

        // Плавная кривая через точки (касательные как у Catmull-Rom, края плоские).
        public static AnimationCurve SmoothCurve(float[] times, float[] values)
        {
            int n = Mathf.Min(times.Length, values.Length);
            Keyframe[] keys = new Keyframe[n];
            for (int i = 0; i < n; i++)
            {
                float tangent = 0f;
                if (i > 0 && i < n - 1)
                {
                    float dt = times[i + 1] - times[i - 1];
                    if (dt > 1e-5f) tangent = (values[i + 1] - values[i - 1]) / dt;
                }
                keys[i] = new Keyframe(times[i], values[i], tangent, tangent);
            }
            return new AnimationCurve(keys);
        }
    }
}
