#if !EXPERIMENT_BUILD
using UnityEngine;
using UnityEngine.UI;

namespace AIB
{
    public class HUDManager : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasScaler scaler;
        
        private GameObject vitalsPanelObj;
        private GameObject neurochemPanelObj;
        private GameObject predictionPanelObj;
        private GameObject minimapPanelObj;
        private GameObject observerControlsPanelObj;

        private void Awake()
        {
            CreateCanvas();
            CreatePanels();
        }

        private void CreateCanvas()
        {
            GameObject canvasObj = new GameObject("AIB_HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);
            
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanels()
        {
            vitalsPanelObj = CreatePanel("VitalsPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(300, 200));
            vitalsPanelObj.AddComponent<VitalsPanel>();

            neurochemPanelObj = CreatePanel("NeurochemPanel", new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(500, 300));
            neurochemPanelObj.AddComponent<NeurochemGraph>();

            predictionPanelObj = CreatePanel("PredictionPanel", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 20), new Vector2(300, 200));
            predictionPanelObj.AddComponent<PredictionErrorPanel>();

            minimapPanelObj = CreatePanel("MinimapPanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(256, 256 + 30));
            minimapPanelObj.AddComponent<MinimapController>();

            observerControlsPanelObj = CreatePanel("ObserverControlsPanel", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(620, 132));
            observerControlsPanelObj.AddComponent<ObserverControlsPanel>();
        }

        private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color32(20, 20, 20, 180);

            return panel;
        }

        private void Update()
        {
            bool showHUD = CameraController.BroadcastModeActive; // AIB-observer-patch-hud // inverted: HUD visible in Observer/Broadcast mode
            
            if (canvas.enabled != showHUD)
            {
                canvas.enabled = showHUD;
            }
        }
    }
}
#endif
