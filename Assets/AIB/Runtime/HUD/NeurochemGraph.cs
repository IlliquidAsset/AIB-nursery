#if !EXPERIMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

namespace AIB
{
    public class NeurochemGraph : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private const int HISTORY_SIZE = 300;
        private RawImage graphImage;
        private Texture2D graphTexture;
        private RectTransform rectTransform;
        private GameObject contentContainer;
        private bool isCollapsed = false;

        private class SignalData
        {
            public string name;
            public Color color;
            public float[] history = new float[HISTORY_SIZE];
            public TextMeshProUGUI legendText;
        }

        private List<SignalData> signals = new List<SignalData>();
        private int currentIndex = 0;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CreateUI();
            InitializeSignals();
        }

        private void CreateUI()
        {
            // Title Bar
            GameObject titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(transform, false);
            RectTransform titleRt = titleBar.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(0, 30);
            Image titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            TextMeshProUGUI titleText = new GameObject("TitleText").AddComponent<TextMeshProUGUI>();
            titleText.transform.SetParent(titleBar.transform, false);
            titleText.text = "Neurochemicals & Signals";
            titleText.fontSize = 16;
            titleText.alignment = TextAlignmentOptions.Left;
            RectTransform ttRt = titleText.rectTransform;
            ttRt.anchorMin = new Vector2(0, 0);
            ttRt.anchorMax = new Vector2(1, 1);
            ttRt.offsetMin = new Vector2(10, 0);
            ttRt.offsetMax = Vector2.zero;

            // Collapse Button
            GameObject collapseBtn = new GameObject("CollapseBtn");
            collapseBtn.transform.SetParent(titleBar.transform, false);
            RectTransform cbRt = collapseBtn.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1, 0.5f);
            cbRt.anchorMax = new Vector2(1, 0.5f);
            cbRt.pivot = new Vector2(1, 0.5f);
            cbRt.anchoredPosition = new Vector2(-5, 0);
            cbRt.sizeDelta = new Vector2(20, 20);
            Image cbImg = collapseBtn.AddComponent<Image>();
            cbImg.color = Color.gray;
            Button btn = collapseBtn.AddComponent<Button>();
            btn.onClick.AddListener(ToggleCollapse);

            // Content Container
            contentContainer = new GameObject("Content");
            contentContainer.transform.SetParent(transform, false);
            RectTransform ccRt = contentContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0, 0);
            ccRt.anchorMax = new Vector2(1, 1);
            ccRt.offsetMin = new Vector2(0, 0);
            ccRt.offsetMax = new Vector2(0, -30);

            // Graph Image
            GameObject graphObj = new GameObject("GraphImage");
            graphObj.transform.SetParent(contentContainer.transform, false);
            RectTransform gRt = graphObj.AddComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0, 0);
            gRt.anchorMax = new Vector2(1, 1);
            gRt.offsetMin = new Vector2(10, 10);
            gRt.offsetMax = new Vector2(-150, -10); // Leave room for legend
            graphImage = graphObj.AddComponent<RawImage>();
            
            graphTexture = new Texture2D(HISTORY_SIZE, 200, TextureFormat.RGBA32, false);
            graphTexture.filterMode = FilterMode.Point;
            graphImage.texture = graphTexture;

            // Legend Container
            GameObject legendObj = new GameObject("Legend");
            legendObj.transform.SetParent(contentContainer.transform, false);
            RectTransform lRt = legendObj.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(1, 0);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.offsetMin = new Vector2(-140, 10);
            lRt.offsetMax = new Vector2(-10, -10);
        }

        private void InitializeSignals()
        {
            AddSignal("Dopamine", Color.yellow);
            AddSignal("Cortisol", Color.red);
            AddSignal("Oxytocin", new Color(1f, 0.4f, 0.7f)); // Pink
            AddSignal("Serotonin", Color.cyan);
            AddSignal("Norepinephrine", new Color(1f, 0.5f, 0f)); // Orange
            AddSignal("Endorphins", Color.green);
            AddSignal("Curiosity", Color.white);
            AddSignal("Stress", new Color(0.5f, 0f, 0f)); // Dark Red
            AddSignal("Plasticity", new Color(0.5f, 0f, 0.5f)); // Purple
            AddSignal("Alertness", new Color(1f, 0.6f, 0f)); // Bright Orange
            AddSignal("Focus", Color.blue);
            AddSignal("Inhibition", Color.gray);
            AddSignal("Bonding", Color.magenta);
        }

        private void AddSignal(string name, Color color)
        {
            SignalData sd = new SignalData { name = name, color = color };
            signals.Add(sd);

            Transform legendParent = contentContainer.transform.Find("Legend");
            
            GameObject itemObj = new GameObject($"Legend_{name}");
            itemObj.transform.SetParent(legendParent, false);
            RectTransform rt = itemObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -signals.Count * 15 + 15);
            rt.sizeDelta = new Vector2(0, 15);

            GameObject colorBox = new GameObject("ColorBox");
            colorBox.transform.SetParent(itemObj.transform, false);
            RectTransform cbRt = colorBox.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0, 0.5f);
            cbRt.anchorMax = new Vector2(0, 0.5f);
            cbRt.pivot = new Vector2(0, 0.5f);
            cbRt.anchoredPosition = new Vector2(5, 0);
            cbRt.sizeDelta = new Vector2(10, 10);
            colorBox.AddComponent<Image>().color = color;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(itemObj.transform, false);
            sd.legendText = textObj.AddComponent<TextMeshProUGUI>();
            sd.legendText.fontSize = 10;
            sd.legendText.alignment = TextAlignmentOptions.Left;
            RectTransform tRt = sd.legendText.rectTransform;
            tRt.anchorMin = new Vector2(0, 0);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.offsetMin = new Vector2(20, 0);
            tRt.offsetMax = Vector2.zero;
            sd.legendText.text = $"{name}: 0.00";
        }

        private void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            contentContainer.SetActive(!isCollapsed);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, isCollapsed ? 30 : 300);
        }

        private void Update()
        {
            if (AbeStateBuffer.Instance == null || isCollapsed) return;

            var state = AbeStateBuffer.Instance.CurrentState ?? AbeStatePayload.Default();

            UpdateSignalData(state);
            DrawGraph();
        }

        private void UpdateSignalData(AbeStatePayload state)
        {
            signals[0].history[currentIndex] = state.dopamine;
            signals[1].history[currentIndex] = state.cortisol;
            signals[2].history[currentIndex] = state.oxytocin;
            signals[3].history[currentIndex] = state.serotonin;
            signals[4].history[currentIndex] = state.norepinephrine;
            signals[5].history[currentIndex] = state.endorphins;
            signals[6].history[currentIndex] = state.curiosity;
            signals[7].history[currentIndex] = state.stress;
            signals[8].history[currentIndex] = state.plasticity;
            signals[9].history[currentIndex] = state.alertness;
            signals[10].history[currentIndex] = state.focus;
            signals[11].history[currentIndex] = state.inhibition;
            signals[12].history[currentIndex] = state.bonding;

            foreach (var sig in signals)
            {
                sig.legendText.text = $"{sig.name}: {sig.history[currentIndex]:F2}";
            }

            currentIndex = (currentIndex + 1) % HISTORY_SIZE;
        }

        private void DrawGraph()
        {
            int width = graphTexture.width;
            int height = graphTexture.height;

            Color32[] pixels = new Color32[width * height];
            Color32 bgColor = new Color32(30, 30, 30, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

            float maxVal = 1f;
            foreach (var sig in signals)
            {
                for (int i = 0; i < HISTORY_SIZE; i++)
                {
                    if (sig.history[i] > maxVal) maxVal = sig.history[i];
                }
            }

            foreach (var sig in signals)
            {
                Color32 col = sig.color;
                for (int x = 0; x < HISTORY_SIZE - 1; x++)
                {
                    int dataIdx1 = (currentIndex + x) % HISTORY_SIZE;
                    int dataIdx2 = (currentIndex + x + 1) % HISTORY_SIZE;

                    float val1 = sig.history[dataIdx1] / maxVal;
                    float val2 = sig.history[dataIdx2] / maxVal;

                    int y1 = Mathf.Clamp(Mathf.RoundToInt(val1 * (height - 1)), 0, height - 1);
                    int y2 = Mathf.Clamp(Mathf.RoundToInt(val2 * (height - 1)), 0, height - 1);

                    DrawLine(pixels, width, height, x, y1, x + 1, y2, col);
                }
            }

            graphTexture.SetPixels32(pixels);
            graphTexture.Apply();
        }

        private void DrawLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 col)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, e2;

            while (true)
            {
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                {
                    pixels[y0 * width + x0] = col;
                }
                if (x0 == x1 && y0 == y1) break;
                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out _);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isCollapsed) return;

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition))
            {
                Vector2 newSize = new Vector2(localPointerPosition.x - rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - localPointerPosition.y);
                newSize.x = Mathf.Max(newSize.x, 300);
                newSize.y = Mathf.Max(newSize.y, 150);
                rectTransform.sizeDelta = newSize;
            }
        }
    }
}
#endif