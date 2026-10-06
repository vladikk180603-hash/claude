using System.Collections.Generic;
using UnityEngine;

namespace EggGame.HitFeel
{
    // Простой пул эффектов попадания. Создаётся автоматически.
    public class HitSparkSpawner : MonoBehaviour
    {
        [Tooltip("Сколько экземпляров создавать заранее на каждый префаб.")]
        [Min(1)] public int prewarmCount = 8;

        [Tooltip("Максимум экземпляров на префаб (если все заняты, берётся самый старый).")]
        [Min(1)] public int maxPerPrefab = 24;

        private class Pool
        {
            public GameObject prefab;
            public readonly Queue<GameObject> free = new Queue<GameObject>();
            public int total;
        }

        private struct Active
        {
            public GameObject instance;
            public Pool pool;
            public float returnTime;
        }

        private static HitSparkSpawner _instance;
        private static bool _quitting;
        private readonly Dictionary<GameObject, Pool> _pools = new Dictionary<GameObject, Pool>();
        private readonly Dictionary<GameObject, ParticleSystem[]> _particles = new Dictionary<GameObject, ParticleSystem[]>();
        private readonly List<Active> _active = new List<Active>(32);

        public static HitSparkSpawner Instance
        {
            get
            {
                if (_instance == null && !_quitting)
                {
                    _instance = FindFirstObjectByType<HitSparkSpawner>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[HitSparkSpawner]");
                        _instance = go.AddComponent<HitSparkSpawner>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void OnApplicationQuit() { _quitting = true; }

        // Заранее создать экземпляры (можно вызвать при загрузке уровня).
        public void Prewarm(GameObject prefab)
        {
            if (prefab == null) return;
            GetPool(prefab);
        }

        public void Spawn(GameObject prefab, Vector3 point, Vector3 normal, float lifetime)
        {
            if (prefab == null) return;
            Pool pool = GetPool(prefab);

            GameObject go = TakeFree(pool);
            if (go == null) return;

            Quaternion rot = normal.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(normal) : Quaternion.identity;
            go.transform.SetPositionAndRotation(point, rot);
            go.SetActive(true);

            ParticleSystem[] systems;
            if (_particles.TryGetValue(go, out systems))
            {
                for (int i = 0; i < systems.Length; i++)
                {
                    if (systems[i] == null) continue;
                    systems[i].Clear(true);
                    systems[i].Play(true);
                }
            }

            Active a;
            a.instance = go;
            a.pool = pool;
            a.returnTime = Time.time + Mathf.Max(0.05f, lifetime);
            _active.Add(a);
        }

        private Pool GetPool(GameObject prefab)
        {
            Pool pool;
            if (_pools.TryGetValue(prefab, out pool)) return pool;
            pool = new Pool { prefab = prefab };
            _pools.Add(prefab, pool);
            int n = Mathf.Min(prewarmCount, maxPerPrefab);
            for (int i = 0; i < n; i++) pool.free.Enqueue(CreateInstance(pool));
            return pool;
        }

        private GameObject CreateInstance(Pool pool)
        {
            GameObject go = Instantiate(pool.prefab, transform);
            go.SetActive(false);
            _particles[go] = go.GetComponentsInChildren<ParticleSystem>(true);
            pool.total++;
            return go;
        }

        private GameObject TakeFree(Pool pool)
        {
            while (pool.free.Count > 0)
            {
                GameObject go = pool.free.Dequeue();
                if (go != null) return go;
                pool.total--; // кто-то уничтожил экземпляр
            }
            if (pool.total < maxPerPrefab) return CreateInstance(pool);

            // Все заняты: переиспользуем самый старый активный.
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].pool == pool && _active[i].instance != null)
                {
                    GameObject go = _active[i].instance;
                    _active.RemoveAt(i);
                    go.SetActive(false);
                    return go;
                }
            }
            return null;
        }

        private void Update()
        {
            float now = Time.time;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Active a = _active[i];
                if (a.instance == null) { _active.RemoveAt(i); a.pool.total--; continue; }
                if (now < a.returnTime) continue;
                a.instance.SetActive(false);
                a.pool.free.Enqueue(a.instance);
                _active.RemoveAt(i);
            }
        }
    }
}
