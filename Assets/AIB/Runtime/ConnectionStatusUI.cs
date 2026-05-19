#if !EXPERIMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIB
{
    public class ConnectionStatusUI : MonoBehaviour
    {
        private TextMeshProUGUI _statusText;
        private Image _backgroundPanel;
        private Canvas _canvas;

        private void Start()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("ConnectionStatusCanvas");
            canvasObj.transform.SetParent(transform);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100; // Ensure it's on top
            
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Background Panel
            GameObject panelObj = new GameObject("BackgroundPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            _backgroundPanel = panelObj.AddComponent<Image>();
            _backgroundPanel.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(150, 30);

            // Create Text
            GameObject textObj = new GameObject("StatusText");
            textObj.transform.SetParent(panelObj.transform, false);
            _statusText = textObj.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize = 14;
            _statusText.alignment = TextAlignmentOptions.Center;
            _statusText.enableWordWrapping = false;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
        }

        private void Update()
        {
            if (_statusText == null) return;

            string status = AbeStateBuffer.Instance.ConnectionStatus;
            _statusText.text = status;

            if (status == "Live")
            {
                _statusText.color = Color.green;
            }
            else if (status == "Connecting...")
            {
                _statusText.color = Color.yellow;
            }
            else if (status == "Reconnecting...")
            {
                _statusText.color = Color.red;
            }
            else
            {
                _statusText.color = Color.white;
            }
        }
    }
}
#endif