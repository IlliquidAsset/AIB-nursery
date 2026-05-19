#if !EXPERIMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace AIB
{
    public class VitalsPanel : MonoBehaviour
    {
        private Image healthBarFill;
        private Image lavaBarFill;
        private TextMeshProUGUI lavaDeltaText;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI connectionText;

        private Queue<float> lavaDeltaBuffer = new Queue<float>();
        private const int LAVA_BUFFER_SIZE = 10;

        private void Awake()
        {
            CreateUI();
        }

        private void CreateUI()
        {
            // Health Bar
            GameObject healthBgObj = CreateBarBackground("HealthBarBg", new Vector2(10, -10), new Vector2(280, 20));
            healthBarFill = CreateBarFill("HealthBarFill", healthBgObj.transform, Color.green);

            // Lava Bar
            GameObject lavaBgObj = CreateBarBackground("LavaBarBg", new Vector2(10, -40), new Vector2(240, 20));
            lavaBarFill = CreateBarFill("LavaBarFill", lavaBgObj.transform, new Color(1f, 0.5f, 0f));

            // Lava Delta
            GameObject lavaDeltaObj = new GameObject("LavaDeltaText");
            lavaDeltaObj.transform.SetParent(transform, false);
            lavaDeltaText = lavaDeltaObj.AddComponent<TextMeshProUGUI>();
            lavaDeltaText.fontSize = 14;
            lavaDeltaText.alignment = TextAlignmentOptions.Left;
            RectTransform ldRt = lavaDeltaText.rectTransform;
            ldRt.anchorMin = new Vector2(0, 1);
            ldRt.anchorMax = new Vector2(0, 1);
            ldRt.pivot = new Vector2(0, 1);
            ldRt.anchoredPosition = new Vector2(260, -40);
            ldRt.sizeDelta = new Vector2(40, 20);

            // Stats Text
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(transform, false);
            statsText = statsObj.AddComponent<TextMeshProUGUI>();
            statsText.fontSize = 16;
            statsText.alignment = TextAlignmentOptions.TopLeft;
            RectTransform stRt = statsText.rectTransform;
            stRt.anchorMin = new Vector2(0, 1);
            stRt.anchorMax = new Vector2(0, 1);
            stRt.pivot = new Vector2(0, 1);
            stRt.anchoredPosition = new Vector2(10, -70);
            stRt.sizeDelta = new Vector2(280, 100);

            // Connection Text
            GameObject connObj = new GameObject("ConnectionText");
            connObj.transform.SetParent(transform, false);
            connectionText = connObj.AddComponent<TextMeshProUGUI>();
            connectionText.fontSize = 14;
            connectionText.alignment = TextAlignmentOptions.BottomLeft;
            RectTransform ctRt = connectionText.rectTransform;
            ctRt.anchorMin = new Vector2(0, 0);
            ctRt.anchorMax = new Vector2(0, 0);
            ctRt.pivot = new Vector2(0, 0);
            ctRt.anchoredPosition = new Vector2(10, 10);
            ctRt.sizeDelta = new Vector2(280, 20);
        }

        private GameObject CreateBarBackground(string name, Vector2 pos, Vector2 size)
        {
            GameObject bg = new GameObject(name);
            bg.transform.SetParent(transform, false);
            RectTransform rt = bg.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image img = bg.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            return bg;
        }

        private Image CreateBarFill(string name, Transform parent, Color color)
        {
            GameObject fill = new GameObject(name);
            fill.transform.SetParent(parent, false);
            RectTransform rt = fill.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = fill.AddComponent<Image>();
            img.color = color;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            return img;
        }

        private void Update()
        {
            if (AbeStateBuffer.Instance == null) return;

            var state = AbeStateBuffer.Instance.CurrentState ?? AbeStatePayload.Default();

            // Health
            float healthPct = Mathf.Clamp01(state.health / 100f);
            healthBarFill.fillAmount = healthPct;
            healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPct);

            // Lava
            lavaBarFill.fillAmount = Mathf.Clamp01(state.lavaDistance / 100f); // Assuming max distance 100
            
            lavaDeltaBuffer.Enqueue(state.lavaDistanceDelta);
            if (lavaDeltaBuffer.Count > LAVA_BUFFER_SIZE)
            {
                lavaDeltaBuffer.Dequeue();
            }

            float avgDelta = 0f;
            foreach (float d in lavaDeltaBuffer) avgDelta += d;
            if (lavaDeltaBuffer.Count > 0) avgDelta /= lavaDeltaBuffer.Count;

            lavaDeltaText.text = avgDelta > 0 ? $"<color=green>+{avgDelta:F1}</color>" : (avgDelta < 0 ? $"<color=red>{avgDelta:F1}</color>" : "0.0");

            // Stats
            statsText.text = $"Deaths: {state.deaths}\nEpisode: {state.episode}\nPhase: {state.phase}\nTick: {state.tick}";

            // Connection
            bool connected = AbeStateBuffer.Instance.IsConnected;
            string status = AbeStateBuffer.Instance.ConnectionStatus;
            connectionText.text = $"Status: {status}";
            connectionText.color = connected ? Color.green : Color.red;
        }
    }
}
#endif