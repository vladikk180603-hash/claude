using UnityEngine;

namespace EggGame.SwordCombo
{
    // Покачивание камеры от замаха. Ставится на ОТДЕЛЬНЫЙ пустой объект между CameraShakeRoot и камерой.
    // HitFeel.CameraShake не трогает: каждый скрипт двигает только свой объект.
    public class SwingCameraSway : MonoBehaviour
    {
        [Tooltip("Общий множитель покачивания (0 = выключить).")]
        [Range(0f, 2f)] public float multiplier = 1f;

        [Tooltip("Ставить только у ЛОКАЛЬНОГО игрока. Тогда этот экземпляр становится SwingCameraSway.Local.")]
        public bool registerAsLocal = true;

        public static SwingCameraSway Local { get; private set; }

        private Vector3 _basePos;
        private Quaternion _baseRot;
        private Vector3 _extraPos;
        private Vector3 _extraEuler;
        private bool _dirty;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
        }

        private void OnEnable() { if (registerAsLocal) Local = this; }

        private void OnDisable()
        {
            if (Local == this) Local = null;
            _extraPos = Vector3.zero;
            _extraEuler = Vector3.zero;
            _dirty = false;
            transform.localPosition = _basePos;
            transform.localRotation = _baseRot;
        }

        // Добавка на ЭТОТ кадр (локальные оси). Вызывать каждый кадр из Update. Вызовы складываются.
        public void AddOffset(Vector3 localPositionOffset, Vector3 localEulerOffset)
        {
            _extraPos += localPositionOffset;
            _extraEuler += localEulerOffset;
        }

        private void LateUpdate()
        {
            bool has = _extraPos != Vector3.zero || _extraEuler != Vector3.zero;
            if (!has)
            {
                if (_dirty)
                {
                    transform.localPosition = _basePos;
                    transform.localRotation = _baseRot;
                    _dirty = false;
                }
                return;
            }
            transform.localPosition = _basePos + _extraPos * multiplier;
            transform.localRotation = _baseRot * Quaternion.Euler(_extraEuler * multiplier);
            _dirty = true;
            _extraPos = Vector3.zero;
            _extraEuler = Vector3.zero;
        }
    }
}
