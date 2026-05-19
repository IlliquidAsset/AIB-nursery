#if !EXPERIMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace AIB
{
    public class PredictionErrorPanel : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private const int HISTORY_SIZE = 100;
        private RawImage graphImage;
        private Texture2D graphTexture;
        private RectTransform rectTransform;
        private GameObject contentContainer;
        private bool isCollapsed = false;

        private TextMeshProUGUI errorText;
        private TextMeshProUGUI rewardText;

        private float[] history = new float[HISTORY_SIZE];
        private int currentIndex = 0;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CreateUI();
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
            titleText.text = "Prediction Error";
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

            // Error Text
            GameObject errObj = new GameObject("ErrorText");
            errObj.transform.SetParent(contentContainer.transform, false);
            errorText = errObj.AddComponent<TextMeshProUGUI>();
            errorText.fontSize = 36;
            errorText.alignment = TextAlignmentOptions.Center;
            RectTransform errRt = errorText.rectTransform;
            errRt.anchorMin = new Vector2(0, 1);
            errRt.anchorMax = new Vector2(1, 1);
            errRt.pivot = new Vector2(0.5f, 1);
            errRt.anchoredPosition = new Vector2(0, -10);
            errRt.sizeDelta = new Vector2(0, 40);

            // Rewards Text
            GameObject rewObj = new GameObject("RewardsText");
            rewObj.transform.SetParent(contentContainer.transform, false);
            rewardText = rewObj.AddComponent<TextMeshProUGUI>();
            rewardText.fontSize = 14;
            rewardText.alignment = TextAlignmentOptions.Center;
            RectTransform rewRt = rewardText.rectTransform;
            rewRt.anchorMin = new Vector2(0, 1);
            rewRt.anchorMax = new Vector2(1, 1);
            rewRt.pivot = new Vector2(0.5f, 1);
            rewRt.anchoredPosition = new Vector2(0, -50);
            rewRt.sizeDelta = new Vector2(0, 60);

            // Graph Image
            GameObject graphObj = new GameObject("GraphImage");
            graphObj.transform.SetParent(contentContainer.transform, false);
            RectTransform gRt = graphObj.AddComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0, 0);
            gRt.anchorMax = new Vector2(1, 0);
            gRt.pivot = new Vector2(0.5f, 0);
            gRt.anchoredPosition = new Vector2(0, 10);
            gRt.sizeDelta = new Vector2(-20, 80);
            graphImage = graphObj.AddComponent<RawImage>();
            
            graphTexture = new Texture2D(HISTORY_SIZE, 80, TextureFormat.RGBA32, false);
            graphTexture.filterMode = FilterMode.Point;
            graphImage.texture = graphTexture;
        }

        private void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            contentContainer.SetActive(!isCollapsed);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, isCollapsed ? 30 : 200);
        }

        private void Update()
        {
            if (AbeStateBuffer.Instance == null || isCollapsed) return;

            var state = AbeStateBuffer.Instance.CurrentState ?? AbeStatePayload.Default();

            errorText.text = state.predictionError.ToString("F4");
            rewardText.text = $"Reward: {state.rewardThisTick:F2}\nNatural: {state.naturalReward:F2}\nShaped: {state.shapedReward:F2}";

            history[currentIndex] = state.predictionError;
            currentIndex = (currentIndex + 1) % HISTORY_SIZE;

            DrawGraph();
        }

        private void DrawGraph()
        {
            int width = graphTexture.width;
            int height = graphTexture.height;

            Color32[] pixels = new Color32[width * height];
            Color32 bgColor = new Color32(30, 30, 30, 255);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

            float maxVal = 1f;
            for (int i = 0; i < HISTORY_SIZE; i++)
            {
                if (Mathf.Abs(history[i]) > maxVal) maxVal = Mathf.Abs(history[i]);
            }

            Color32 col = Color.cyan;
            for (int x = 0; x < HISTORY_SIZE - 1; x++)
            {
                int dataIdx1 = (currentIndex + x) % HISTORY_SIZE;
                int dataIdx2 = (currentIndex + x + 1) % HISTORY_SIZE;

                // Map -maxVal..maxVal to 0..1
                float val1 = (history[dataIdx1] + maxVal) / (2f * maxVal);
                float val2 = (history[dataIdx2] + maxVal) / (2f * maxVal);

                int y1 = Mathf.Clamp(Mathf.RoundToInt(val1 * (height - 1)), 0, height - 1);
                int y2 = Mathf.Clamp(Mathf.RoundToInt(val2 * (height - 1)), 0, height - 1);

                DrawLine(pixels, width, height, x, y1, x + 1, y2, col);
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
                Vector2 newSize = new Vector2(rectTransform.anchoredPosition.x - localPointerPosition.x, rectTransform.anchoredPosition.y - localPointerPosition.y);
                newSize.x = Mathf.Max(newSize.x, 200);
                newSize.y = Mathf.Max(newSize.y, 150);
                rectTransform.sizeDelta = newSize;
            }
        }
    }
}
#endif