using UnityEngine;
using UnityEngine.UI;

namespace AIB.Runtime
{
    public class VocalWaveformGraph : MonoBehaviour
    {
        [SerializeField] private int graphWidth = 256;
        [SerializeField] private int graphHeight = 64;
        [SerializeField] private Color waveformColor = new Color(0.3f, 0.8f, 0.5f, 0.9f);

        private RawImage _graphImage;
        private Texture2D _texture;
        private VocalAudioPlayer _audioPlayer;

        private void Awake()
        {
            GameObject imgObj = new GameObject("VocalWaveformImage");
            imgObj.transform.SetParent(transform, false);
            RectTransform rt = imgObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(graphWidth, graphHeight);

            _graphImage = imgObj.AddComponent<RawImage>();
            _texture = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;

            ClearTexture();

            _graphImage.texture = _texture;
            _audioPlayer = FindFirstObjectByType<VocalAudioPlayer>();
        }

        private void Update()
        {
            if (_audioPlayer == null)
            {
                _audioPlayer = FindFirstObjectByType<VocalAudioPlayer>();
                return;
            }

            RenderWaveform();
        }

        private void RenderWaveform()
        {
            float[] samples = _audioPlayer.GetSamples();
            if (samples == null || samples.Length == 0) return;

            ClearTexture();

            int step = Mathf.Max(1, samples.Length / graphWidth);

            for (int x = 0; x < graphWidth; x++)
            {
                int sampleStart = x * step;
                float maxAmplitude = 0f;

                for (int i = 0; i < step && sampleStart + i < samples.Length; i++)
                {
                    float a = Mathf.Abs(samples[sampleStart + i]);
                    if (a > maxAmplitude) maxAmplitude = a;
                }

                int barHeight = Mathf.RoundToInt(maxAmplitude * graphHeight);
                barHeight = Mathf.Clamp(barHeight, 0, graphHeight);

                int yMid = graphHeight / 2;
                for (int y = yMid - barHeight; y <= yMid + barHeight; y++)
                {
                    if (y >= 0 && y < graphHeight)
                        _texture.SetPixel(x, y, waveformColor);
                }
            }

            _texture.Apply();
        }

        private void ClearTexture()
        {
            Color[] pixels = new Color[graphWidth * graphHeight];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            _texture.SetPixels(pixels);
            _texture.Apply();
        }
    }
}
