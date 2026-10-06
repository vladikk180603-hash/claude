using System.Collections.Generic;
using UnityEngine;

namespace EggGame.HitFeel
{
    // Локальный Hit Stop: временно меняет Animator.speed. Time.timeScale НЕ трогает.
    // Создаётся автоматически при первом обращении.
    public class HitStopService : MonoBehaviour
    {
        // Максимальная суммарная длительность замирания одного аниматора.
        public const float MaxTotalDuration = 0.15f;

        private struct Entry
        {
            public Animator animator;
            public float originalSpeed;
            public float startTime;
            public float endTime;
        }

        private static HitStopService _instance;
        private static bool _quitting;
        private readonly List<Entry> _entries = new List<Entry>(16);

        public static HitStopService Instance
        {
            get
            {
                if (_instance == null && !_quitting)
                {
                    _instance = FindFirstObjectByType<HitStopService>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[HitStopService]");
                        _instance = go.AddComponent<HitStopService>();
                    }
                }
                return _instance;
            }
        }

        // Удобный статический вызов.
        public static void Request(Animator[] animators, float[] speeds, float duration)
        {
            var s = Instance;
            if (s != null) s.RequestStop(animators, speeds, duration);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit() { _quitting = true; }

        // animators[i] замедляется до speeds[i] на duration секунд.
        public void RequestStop(Animator[] animators, float[] speeds, float duration)
        {
            if (animators == null || duration <= 0f) return;
            float now = Time.time;

            for (int i = 0; i < animators.Length; i++)
            {
                Animator a = animators[i];
                if (a == null) continue;
                float speed = (speeds != null && i < speeds.Length) ? Mathf.Max(0f, speeds[i]) : 0f;

                int idx = FindIndex(a);
                if (idx >= 0)
                {
                    // Уже замедлен: продлеваем, но не дольше лимита от первого запуска.
                    Entry e = _entries[idx];
                    e.endTime = Mathf.Min(e.startTime + MaxTotalDuration, Mathf.Max(e.endTime, now + duration));
                    _entries[idx] = e;
                    a.speed = Mathf.Min(a.speed, speed);
                }
                else
                {
                    Entry e;
                    e.animator = a;
                    e.originalSpeed = a.speed; // запоминаем исходную скорость
                    e.startTime = now;
                    e.endTime = now + Mathf.Min(duration, MaxTotalDuration);
                    _entries.Add(e);
                    a.speed = speed;
                }
            }
        }

        // Немедленно вернуть все скорости (например, при смерти игрока).
        public void ReleaseAll()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry e = _entries[i];
                if (e.animator != null) e.animator.speed = e.originalSpeed;
            }
            _entries.Clear();
        }

        private int FindIndex(Animator a)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].animator == a) return i;
            return -1;
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                Entry e = _entries[i];
                if (e.animator == null) { _entries.RemoveAt(i); continue; } // цель уничтожена
                if (now >= e.endTime)
                {
                    e.animator.speed = e.originalSpeed;
                    _entries.RemoveAt(i);
                }
            }
        }

        private void OnDisable() { ReleaseAll(); }
    }
}
