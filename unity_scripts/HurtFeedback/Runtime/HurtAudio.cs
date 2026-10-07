using UnityEngine;

namespace EggGame.HurtFeedback
{
    // Звук при уроне: «удар в тело» + вскрик; при малом здоровье — сердце и приглушение высоких частот.
    public class HurtAudio : MonoBehaviour
    {
        private PlayerHurtFeedbackConfig _cfg;
        private AudioSource _oneShots;
        private AudioSource _heartbeat;
        private AudioLowPassFilter _lowPass;
        private float _lastScream = -999f;
        private float _lowHealth;
        private float _currentCutoff = 22000f;
        private int _lastImpact = -1;
        private int _lastScreamClip = -1;

        public void Init(PlayerHurtFeedbackConfig cfg)
        {
            _cfg = cfg;
            _oneShots = gameObject.AddComponent<AudioSource>();
            _oneShots.playOnAwake = false;
            _oneShots.spatialBlend = 0f; // 2D: это «мои» звуки

            _heartbeat = gameObject.AddComponent<AudioSource>();
            _heartbeat.playOnAwake = false;
            _heartbeat.loop = true;
            _heartbeat.spatialBlend = 0f;
            _heartbeat.bypassListenerEffects = true; // сердце не приглушаем
            _heartbeat.volume = 0f;
        }

        public void PlayHurt(float intensity)
        {
            if (_cfg == null || _oneShots == null || _cfg.audioMultiplier <= 0f) return;
            float vol = _cfg.audioMultiplier * Mathf.Lerp(0.6f, 1f, intensity);

            AudioClip impact = Pick(_cfg.impactClips, ref _lastImpact);
            if (impact != null)
            {
                _oneShots.pitch = Random.Range(0.93f, 1.07f);
                _oneShots.PlayOneShot(impact, _cfg.impactVolume * vol);
            }

            if (Time.time - _lastScream >= _cfg.screamCooldown)
            {
                AudioClip scream = Pick(_cfg.screamClips, ref _lastScreamClip);
                if (scream != null)
                {
                    _oneShots.PlayOneShot(scream, _cfg.screamVolume * vol);
                    _lastScream = Time.time;
                }
            }
        }

        // 0 = здоровья достаточно, 1 = почти мёртв.
        public void SetLowHealth(float factor01)
        {
            _lowHealth = Mathf.Clamp01(factor01);
        }

        private void Update()
        {
            if (_cfg == null) return;
            float dt = Time.deltaTime;

            // Сердце.
            if (_heartbeat != null)
            {
                float targetVol = _cfg.heartbeatClip != null ? _cfg.heartbeatVolume * _cfg.audioMultiplier * _lowHealth : 0f;
                _heartbeat.volume = Mathf.MoveTowards(_heartbeat.volume, targetVol, dt * 1.5f);
                if (_heartbeat.volume > 0.001f)
                {
                    if (_heartbeat.clip != _cfg.heartbeatClip) _heartbeat.clip = _cfg.heartbeatClip;
                    if (!_heartbeat.isPlaying && _heartbeat.clip != null) _heartbeat.Play();
                }
                else if (_heartbeat.isPlaying) _heartbeat.Stop();
            }

            // Приглушение: частота среза от 22000 до lowHealthLowPassHz.
            float target = Mathf.Lerp(22000f, _cfg.lowHealthLowPassHz, _lowHealth * _cfg.audioMultiplier);
            _currentCutoff = Mathf.MoveTowards(_currentCutoff, target, dt * 30000f);
            ApplyLowPass(_currentCutoff);
        }

        private void ApplyLowPass(float cutoff)
        {
            bool active = cutoff < 21900f;

            if (_cfg.mixer != null && !string.IsNullOrEmpty(_cfg.mixerLowPassParam))
            {
                _cfg.mixer.SetFloat(_cfg.mixerLowPassParam, cutoff);
                return;
            }

            if (_lowPass == null)
            {
                if (!active) return;
                AudioListener listener = FindFirstObjectByType<AudioListener>();
                if (listener == null) return;
                _lowPass = listener.GetComponent<AudioLowPassFilter>();
                if (_lowPass == null) _lowPass = listener.gameObject.AddComponent<AudioLowPassFilter>();
            }
            _lowPass.cutoffFrequency = cutoff;
            if (_lowPass.enabled != active) _lowPass.enabled = active;
        }

        private void OnDisable()
        {
            if (_lowPass != null) { _lowPass.cutoffFrequency = 22000f; _lowPass.enabled = false; }
            if (_cfg != null && _cfg.mixer != null && !string.IsNullOrEmpty(_cfg.mixerLowPassParam))
                _cfg.mixer.SetFloat(_cfg.mixerLowPassParam, 22000f);
            _currentCutoff = 22000f;
            if (_heartbeat != null) { _heartbeat.Stop(); _heartbeat.volume = 0f; }
        }

        // Случайный клип, не повторяя предыдущий подряд.
        private static AudioClip Pick(AudioClip[] clips, ref int last)
        {
            if (clips == null || clips.Length == 0) return null;
            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == last) i = (i + 1) % clips.Length;
            last = i;
            return clips[i];
        }
    }
}
