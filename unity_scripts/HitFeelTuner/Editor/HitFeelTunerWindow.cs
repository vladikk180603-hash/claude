using UnityEditor;
using UnityEngine;
using EggGame.HitFeel;

namespace EggGame.HitFeelTuner
{
    // Окно «Настройка удара»: понятные ползунки для HitFeelProfile + кнопки проверки в режиме Play.
    // Меню: Tools → Egg Game → Hit Feel → Настройка удара (Tuner).
    // Файлы HitFeel не меняет — только значения в выбранном профиле (ассете).
    public class HitFeelTunerWindow : EditorWindow
    {
        private const string PrefKey = "EggGame.HitFeelTuner.ProfileGuid";
        private const float BasePos = 0.03f;    // «сила тряски ×1»
        private const float BaseRoll = 1.2f;
        private const float BaseRecoil = 0.06f;

        // Порядок: hitStop, attackerSpeed, targetSpeed, kbDist, kbDur, kbUp,
        //          shakePos, shakeRoll, shakeRecoil, shakeDur, shakeFreq, lungeDist, lungeDur, lungeDamp
        private static readonly float[] Light   = { 0.035f, 0.1f, 0.10f, 0.6f, 0.15f, 0.00f, 0.015f, 0.6f, 0.03f, 0.12f, 34f, 0.5f, 0.12f, 10f };
        private static readonly float[] Normal  = { 0.060f, 0.0f, 0.05f, 1.2f, 0.20f, 0.00f, 0.030f, 1.2f, 0.06f, 0.18f, 28f, 0.8f, 0.18f, 8f };
        private static readonly float[] Heavy   = { 0.090f, 0.0f, 0.02f, 2.0f, 0.28f, 0.15f, 0.060f, 2.5f, 0.10f, 0.26f, 22f, 1.1f, 0.24f, 6f };
        private static readonly float[] Cartoon = { 0.110f, 0.0f, 0.00f, 2.8f, 0.30f, 0.40f, 0.080f, 3.5f, 0.12f, 0.30f, 18f, 1.2f, 0.22f, 7f };

        private HitFeelProfile _profile;
        private float _weight = 0.5f;
        private bool _advanced;
        private Vector2 _scroll;
        private bool _autoHit;
        private float _autoInterval = 1f;
        private double _nextAuto;
        private string _snapshot;
        private string _status = "";
        private Editor _profileEditor;
        private GUIStyle _hint;
        private GUIStyle _header;

        [MenuItem("Tools/Egg Game/Hit Feel/Настройка удара (Tuner)")]
        public static void Open()
        {
            var w = GetWindow<HitFeelTunerWindow>();
            w.titleContent = new GUIContent("Hit Feel Tuner");
            w.minSize = new Vector2(380f, 520f);
            w.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Tick;
            string guid = EditorPrefs.GetString(PrefKey, "");
            if (!string.IsNullOrEmpty(guid))
                _profile = AssetDatabase.LoadAssetAtPath<HitFeelProfile>(AssetDatabase.GUIDToAssetPath(guid));
            if (_profile == null)
            {
                string[] found = AssetDatabase.FindAssets("t:HitFeelProfile");
                if (found.Length > 0) _profile = AssetDatabase.LoadAssetAtPath<HitFeelProfile>(AssetDatabase.GUIDToAssetPath(found[0]));
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            if (_profile != null) AssetDatabase.SaveAssetIfDirty(_profile);
            if (_profileEditor != null) DestroyImmediate(_profileEditor);
        }

        private void OnGUI()
        {
            if (_hint == null)
            {
                _hint = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
                _header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // ---------- Профиль ----------
            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            _profile = (HitFeelProfile)EditorGUILayout.ObjectField("Профиль удара", _profile, typeof(HitFeelProfile), false);
            if (EditorGUI.EndChangeCheck() && _profile != null)
                EditorPrefs.SetString(PrefKey, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_profile)));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Взять у игрока в сцене")) TakeFromPlayer();
            if (GUILayout.Button("Создать новый профиль")) CreateProfile();
            EditorGUILayout.EndHorizontal();

            if (_profile == null)
            {
                EditorGUILayout.HelpBox("Выберите профиль (например, Sword_HitFeel) или нажмите «Создать новый профиль».", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            // ---------- 1. Вес удара ----------
            Section("1. Общий вес удара");
            EditorGUILayout.LabelField("Один ползунок меняет всё сразу. Потом подправьте детали ниже.", _hint);
            EditorGUI.BeginChangeCheck();
            _weight = EditorGUILayout.Slider(_weight, 0f, 1.5f);
            Rect r = GUILayoutUtility.GetRect(10f, 14f);
            DrawScaleLabels(r);
            if (EditorGUI.EndChangeCheck()) { Record(); ApplyWeight(_weight); }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Лёгкий")) { _weight = 0f; Record(); Apply(Light); }
            if (GUILayout.Button("Обычный")) { _weight = 0.5f; Record(); Apply(Normal); }
            if (GUILayout.Button("Тяжёлый")) { _weight = 1f; Record(); Apply(Heavy); }
            if (GUILayout.Button("Мультяшный")) { _weight = 1.5f; Record(); Apply(Cartoon); }
            EditorGUILayout.EndHorizontal();

            // ---------- 2. Основные ползунки ----------
            Section("2. Основные ползунки");
            HitFeelProfile p = _profile;

            float ms = Slider("Замирание при попадании (мс)", "Пауза анимации в момент удара. 30 — щелчок, 60 — сочно, 100+ — очень тяжело.",
                              p.hitStopSeconds * 1000f, 0f, 150f);
            if (Changed()) p.hitStopSeconds = Mathf.Round(ms) / 1000f;

            float ts = Slider("Враг во время замирания", "0 — враг застывает как статуя, 0.3 — лишь притормаживает.",
                              p.targetAnimSpeedDuringStop, 0f, 0.5f);
            if (Changed()) p.targetAnimSpeedDuringStop = ts;

            float kb = Slider("Отброс врага (м)", "На сколько метров отлетает враг.", p.knockbackDistance, 0f, 3f);
            if (Changed()) p.knockbackDistance = kb;

            float kd = Slider("Время отброса (с)", "Меньше — резкий толчок, больше — плавный отъезд.", p.knockbackDuration, 0.05f, 0.5f);
            if (Changed()) p.knockbackDuration = kd;

            float up = Slider("Подброс вверх (м)", "Только для врагов без NavMeshAgent. Мультяшный «подпрыг».", p.knockbackUp, 0f, 1f);
            if (Changed()) p.knockbackUp = up;

            float shake = Slider("Тряска камеры (сила ×)", "Дрожание, крен и отдача камеры вместе. 1 — стандарт, 0 — выключить.",
                                 BasePos > 0f ? p.shakePosAmplitude / BasePos : 1f, 0f, 3f);
            if (Changed())
            {
                p.shakePosAmplitude = BasePos * shake;
                p.shakeRollAmplitudeDeg = BaseRoll * shake;
                p.shakeRecoilBack = BaseRecoil * shake;
            }

            float sd = Slider("Длительность тряски (с)", "Как долго трясётся камера.", p.shakeDuration, 0.05f, 0.5f);
            if (Changed()) p.shakeDuration = sd;

            float ld = Slider("Рывок вперёд при замахе (м)", "Насколько игрока «тянет» за мечом.", p.lungeDistance, 0f, 2f);
            if (Changed()) p.lungeDistance = ld;

            float lp = Slider("Резкость рывка", "Больше — резкий старт и быстрая остановка.", p.lungeDamping, 2f, 15f);
            if (Changed()) p.lungeDamping = lp;

            float vol = Slider("Громкость звука удара", "Если звук задан в профиле.", p.audioVolume, 0f, 1f);
            if (Changed()) p.audioVolume = vol;

            // ---------- 3. Проверка ----------
            Section("3. Проверка (в режиме Play)");
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Нажмите Play и встаньте рядом с врагом или манекеном (с KnockbackReceiver). " +
                                        "Кнопки ниже ударят без мыши. Ползунки можно двигать прямо во время игры — изменения сохраняются.",
                                        MessageType.Info);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ударить ближайшего врага", GUILayout.Height(28))) HitNearest();
                if (GUILayout.Button("Замах (рывок)", GUILayout.Height(28))) Swing();
                if (GUILayout.Button("Только тряска", GUILayout.Height(28))) ShakeOnly();
                EditorGUILayout.EndHorizontal();

                _autoHit = EditorGUILayout.ToggleLeft("Бить автоматически (крутите ползунки и смотрите)", _autoHit);
                if (_autoHit) _autoInterval = EditorGUILayout.Slider("Каждые (с)", _autoInterval, 0.3f, 3f);
                if (!string.IsNullOrEmpty(_status)) EditorGUILayout.LabelField(_status, _hint);
            }

            // ---------- 4. Сравнение ----------
            Section("4. Сравнить «до» и «после»");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Запомнить текущее")) { _snapshot = EditorJsonUtility.ToJson(_profile); _status = "Запомнено."; }
            GUI.enabled = !string.IsNullOrEmpty(_snapshot);
            if (GUILayout.Button("Вернуть запомненное")) { Record(); EditorJsonUtility.FromJsonOverwrite(_snapshot, _profile); }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Отменить последнее движение ползунка: Ctrl+Z.", _hint);

            // ---------- 5. Все параметры ----------
            EditorGUILayout.Space(6);
            _advanced = EditorGUILayout.Foldout(_advanced, "Все параметры профиля (для тонкой настройки)", true);
            if (_advanced)
            {
                Editor.CreateCachedEditor(_profile, null, ref _profileEditor);
                if (_profileEditor != null) _profileEditor.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();
        }

        // ---------- Ползунки ----------

        private bool _changed;

        private float Slider(string label, string hint, float value, float min, float max)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float v = EditorGUILayout.Slider(value, min, max);
            _changed = EditorGUI.EndChangeCheck();
            if (_changed) Record();
            EditorGUILayout.LabelField(hint, _hint);
            return v;
        }

        private bool Changed() { bool c = _changed; _changed = false; return c; }

        private void Section(string title)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, _header);
            Rect line = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(line, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        private void DrawScaleLabels(Rect r)
        {
            // Подписи под ползунком веса (слайдер занимает ширину минус поле числа ~55px).
            float w = r.width - 55f;
            string[] names = { "лёгкий", "обычный", "тяжёлый", "мульт." };
            for (int i = 0; i < names.Length; i++)
            {
                float x = r.x + w * (i / 3f) - 20f;
                GUI.Label(new Rect(Mathf.Clamp(x, r.x, r.x + w - 50f), r.y, 60f, r.height), names[i], _hint);
            }
        }

        private void Record()
        {
            if (_profile == null) return;
            Undo.RecordObject(_profile, "Hit Feel Tuner");
            EditorUtility.SetDirty(_profile);
        }

        // ---------- Пресеты ----------

        private void ApplyWeight(float w)
        {
            if (w <= 0.5f) Apply(Lerp(Light, Normal, w / 0.5f));
            else if (w <= 1f) Apply(Lerp(Normal, Heavy, (w - 0.5f) / 0.5f));
            else Apply(Lerp(Heavy, Cartoon, (w - 1f) / 0.5f));
        }

        private static float[] Lerp(float[] a, float[] b, float t)
        {
            var r = new float[a.Length];
            for (int i = 0; i < a.Length; i++) r[i] = Mathf.Lerp(a[i], b[i], t);
            return r;
        }

        private void Apply(float[] v)
        {
            HitFeelProfile p = _profile;
            if (p == null) return;
            p.hitStopSeconds = v[0];
            p.attackerAnimSpeedDuringStop = v[1];
            p.targetAnimSpeedDuringStop = v[2];
            p.knockbackDistance = v[3];
            p.knockbackDuration = v[4];
            p.knockbackUp = v[5];
            p.shakePosAmplitude = v[6];
            p.shakeRollAmplitudeDeg = v[7];
            p.shakeRecoilBack = v[8];
            p.shakeDuration = v[9];
            p.shakeFrequency = v[10];
            p.lungeDistance = v[11];
            p.lungeDuration = v[12];
            p.lungeDamping = v[13];
            EditorUtility.SetDirty(p);
        }

        // ---------- Профиль ----------

        private void TakeFromPlayer()
        {
            var ctrl = Object.FindFirstObjectByType<HitFeelController>();
            if (ctrl != null && ctrl.profile != null) SetProfile(ctrl.profile);
            else _status = "В открытой сцене нет HitFeelController с профилем.";
        }

        private void CreateProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Новый профиль удара", "My_HitFeel", "asset", "Где сохранить профиль?");
            if (string.IsNullOrEmpty(path)) return;
            var p = CreateInstance<HitFeelProfile>();
            AssetDatabase.CreateAsset(p, path);
            AssetDatabase.SaveAssets();
            SetProfile(p);
        }

        private void SetProfile(HitFeelProfile p)
        {
            _profile = p;
            EditorPrefs.SetString(PrefKey, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p)));
            Repaint();
        }

        // ---------- Проверка в Play ----------

        private void Tick()
        {
            if (!_autoHit || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            if (EditorApplication.timeSinceStartup < _nextAuto) return;
            _nextAuto = EditorApplication.timeSinceStartup + _autoInterval;
            HitNearest();
            Repaint();
        }

        private void HitNearest()
        {
            if (_profile == null) return;
            var ctrl = Object.FindFirstObjectByType<HitFeelController>();
            if (ctrl == null) { _status = "Нет HitFeelController в сцене."; return; }

            Vector3 me = ctrl.transform.position;
            KnockbackReceiver best = null;
            float bestDist = 8f * 8f;
            var all = Object.FindObjectsByType<KnockbackReceiver>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                float d = (all[i].transform.position - me).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = all[i]; }
            }

            if (best == null)
            {
                var tester = Object.FindFirstObjectByType<HitFeelTester>();
                if (tester != null) { tester.SimulateHit(); _status = "Врагов рядом нет — удар по тому, что перед камерой."; }
                else _status = "Рядом (8 м) нет врага с KnockbackReceiver.";
                return;
            }

            Collider col = best.GetComponentInChildren<Collider>();
            if (col == null) { _status = best.name + ": нет коллайдера."; return; }

            Vector3 from = me + Vector3.up * 1.2f;
            Vector3 contact = col.ClosestPoint(from);
            if (contact == from) contact = col.bounds.center;
            Vector3 dir = best.transform.position - me;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 1e-6f ? dir.normalized : ctrl.transform.forward;
            Vector3 normal = from - contact;
            normal = normal.sqrMagnitude > 1e-6f ? normal.normalized : -dir;

            ctrl.PlayHit(_profile, new HitInfo(ctrl.transform, col, contact, normal, dir), ctrl.weaponAnimator);
            _status = "Удар по " + best.name + " (" + Mathf.Sqrt(bestDist).ToString("0.0") + " м).";
        }

        private void Swing()
        {
            var ctrl = Object.FindFirstObjectByType<HitFeelController>();
            if (ctrl == null || _profile == null) { _status = "Нет HitFeelController в сцене."; return; }
            ctrl.PlaySwingStart(_profile);
            _status = "Рывок.";
        }

        private void ShakeOnly()
        {
            if (_profile == null) return;
            Camera c = Camera.main;
            CameraShake.ImpulseLocal(_profile, c != null ? c.transform.forward : Vector3.forward);
            _status = CameraShake.Local != null ? "Тряска." : "Нет активного CameraShake.";
        }
    }
}
