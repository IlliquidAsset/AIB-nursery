using UnityEngine;

namespace AIB.Runtime
{
    public class VocalAudioPlayer : MonoBehaviour
    {
        [SerializeField] private int sampleRate = 16000;

        private AudioSource _audioSource;
        private AudioClip _clip;
        private float[] _samples;
        private int _sampleIndex;
        private float _sampleDuration;

        public bool HasVocalData { get; private set; }

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 0f;
            _audioSource.loop = false;
            _audioSource.playOnAwake = false;
        }

        public void PrepareClip(float totalDurationSeconds)
        {
            int totalSamples = Mathf.CeilToInt(sampleRate * totalDurationSeconds);
            _clip = AudioClip.Create("AbeVocal", totalSamples, 1, sampleRate, false);
            _samples = new float[totalSamples];
            _sampleIndex = 0;
            _sampleDuration = totalDurationSeconds;
            HasVocalData = false;
        }

        public void WriteSample(float pitchHz, float volume, float formant, bool gate, int frameCount)
        {
            int samplesPerFrame = Mathf.Max(1, sampleRate / 30);
            samplesPerFrame = Mathf.Min(samplesPerFrame, _samples.Length - _sampleIndex);

            if (gate && volume > 0f && pitchHz > 20f)
            {
                HasVocalData = true;
                for (int i = 0; i < samplesPerFrame; i++)
                {
                    float t = (float)(_sampleIndex + i) / sampleRate;
                    float fundamental = Mathf.Sin(2f * Mathf.PI * pitchHz * t);
                    float harmonic = Mathf.Sin(2f * Mathf.PI * pitchHz * formant * t) * 0.3f;
                    _samples[_sampleIndex + i] = (fundamental + harmonic) * Mathf.Clamp01(volume);
                }
            }

            _sampleIndex += samplesPerFrame;
        }

        public void FinalizeClip()
        {
            if (_clip != null && _samples != null)
            {
                _clip.SetData(_samples, 0);
                _audioSource.clip = _clip;
            }
        }

        public void Play()
        {
            if (_audioSource.clip != null)
                _audioSource.Play();
        }

        public void Stop()
        {
            _audioSource.Stop();
        }

        public float[] GetSamples()
        {
            return _samples;
        }

        public int GetSampleRate()
        {
            return sampleRate;
        }
    }
}
