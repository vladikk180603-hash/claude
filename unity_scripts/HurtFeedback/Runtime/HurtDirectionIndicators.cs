using UnityEngine;
using UnityEngine.UI;

namespace EggGame.HurtFeedback
{
    // Красные дуги вокруг центра экрана, повёрнутые к источнику удара.
    // Дуга следит за источником, даже если игрок поворачивается.
    public class HurtDirectionIndicators : MonoBehaviour
    {
        private struct Arc
        {
            public Image image;
            public Vector3 source;
            public float age;
            public float alpha;
            public bool alive;
        }

        private PlayerHurtFeedbackConfig _cfg;
        private Arc[] _arcs = new Arc[0];
        private Transform _viewer;

        public void Init(PlayerHurtFeedbackConfig cfg, Sprite arcSprite, Transform viewer)
        {
            _cfg = cfg;
            _viewer = viewer;
            int n = Mathf.Max(1, cfg.indicatorMaxCount);
            _arcs = new Arc[n];
            float size = cfg.indicatorRadius * 2f;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("HurtArc_" + i, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(transform, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = Vector2.zero;
                var img = go.GetComponent<Image>();
                img.sprite = arcSprite;
                img.raycastTarget = false;
                img.enabled = false;
                _arcs[i].image = img;
            }
        }

        public void SetViewer(Transform viewer) { _viewer = viewer; }

        public void Show(Vector3 sourceWorldPosition, float intensity)
        {
            if (_cfg == null || _arcs.Length == 0 || _cfg.indicatorMultiplier <= 0f) return;

            // Свободная дуга или самая старая.
            int idx = 0;
            float oldest = -1f;
            for (int i = 0; i < _arcs.Length; i++)
            {
                if (!_arcs[i].alive) { idx = i; oldest = float.MaxValue; break; }
                if (_arcs[i].age > oldest) { oldest = _arcs[i].age; idx = i; }
            }

            _arcs[idx].source = sourceWorldPosition;
            _arcs[idx].age = 0f;
            _arcs[idx].alpha = Mathf.Lerp(_cfg.indicatorMinAlpha, 1f, intensity) * _cfg.indicatorMultiplier;
            _arcs[idx].alive = true;
            _arcs[idx].image.enabled = true;
            UpdateArc(ref _arcs[idx]);
        }

        private void Update()
        {
            if (_cfg == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < _arcs.Length; i++)
            {
                if (!_arcs[i].alive) continue;
                _arcs[i].age += dt;
                if (_arcs[i].age >= _cfg.indicatorLifetime)
                {
                    _arcs[i].alive = false;
                    _arcs[i].image.enabled = false;
                    continue;
                }
                UpdateArc(ref _arcs[i]);
            }
        }

        private void UpdateArc(ref Arc a)
        {
            float angle = 0f;
            if (_viewer != null)
            {
                Vector3 fwd = _viewer.forward; fwd.y = 0f;
                Vector3 dir = a.source - _viewer.position; dir.y = 0f;
                if (fwd.sqrMagnitude > 1e-6f && dir.sqrMagnitude > 1e-6f)
                    angle = Vector3.SignedAngle(fwd, dir, Vector3.up); // + справа
            }
            a.image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);

            float t = Mathf.Clamp01(a.age / _cfg.indicatorLifetime);
            float fade = 1f - t * t;                         // плавное затухание к концу
            float pop = 1f + 0.12f * Mathf.Exp(-a.age * 18f); // лёгкий «толчок» при появлении
            a.image.rectTransform.localScale = new Vector3(pop, pop, 1f);
            Color c = _cfg.indicatorColor;
            c.a = a.alpha * fade;
            a.image.color = c;
        }
    }
}
