#if !EXPERIMENT_BUILD
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace AIB
{
    public class MinimapController : MonoBehaviour, IDragHandler
    {
        private Camera minimapCamera;
        private RenderTexture renderTexture;
        private RawImage minimapImage;
        private RectTransform rectTransform;
        private GameObject contentContainer;
        private bool isCollapsed = false;

        private RectTransform agentIcon;
        private RectTransform stat1Icon;
        private RectTransform stat2Icon;

        private CameraController camController;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            CreateUI();
            SetupCamera();
        }

        private void Start()
        {
            camController = FindAnyObjectByType<CameraController>();
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
            titleText.text = "Minimap";
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

            // Minimap Image
            GameObject mapObj = new GameObject("MapImage");
            mapObj.transform.SetParent(contentContainer.transform, false);
            RectTransform mRt = mapObj.AddComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0, 0);
            mRt.anchorMax = new Vector2(1, 1);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            minimapImage = mapObj.AddComponent<RawImage>();

            // Icons
            agentIcon = CreateIcon("AgentIcon", Color.green, 10, contentContainer.transform);
            stat1Icon = CreateIcon("Stat1Icon", Color.blue, 8, contentContainer.transform);
            stat2Icon = CreateIcon("Stat2Icon", Color.red, 8, contentContainer.transform);
        }

        private RectTransform CreateIcon(string name, Color color, float size, Transform parent)
        {
            GameObject iconObj = new GameObject(name);
            iconObj.transform.SetParent(parent, false);
            RectTransform rt = iconObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            Image img = iconObj.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        private void SetupCamera()
        {
            GameObject camObj = new GameObject("MinimapCamera");
            minimapCamera = camObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            minimapCamera.targetTexture = renderTexture;
            minimapImage.texture = renderTexture;

            // Look down
            minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        private void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            contentContainer.SetActive(!isCollapsed);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, isCollapsed ? 30 : 256 + 30);
        }

        private void Update()
        {
            if (AbeStateBuffer.Instance == null || isCollapsed || camController == null) return;

            var state = AbeStateBuffer.Instance.CurrentState ?? AbeStatePayload.Default();
            Vector3 agentPos = state.Position;

            if (CameraController.CurrentMode == CameraMode.OTS || CameraController.CurrentMode == CameraMode.FirstPerson)
            {
                minimapCamera.orthographicSize = 15f;
                minimapCamera.transform.position = new Vector3(agentPos.x, 50f, agentPos.z);
                minimapCamera.transform.rotation = Quaternion.Euler(90, state.rotationY, 0);
            }
            else
            {
                minimapCamera.orthographicSize = 40f; // Full arena
                minimapCamera.transform.position = new Vector3(0, 50f, 0);
                minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            }

            UpdateIcons(agentPos, state.rotationY);
        }

        private void UpdateIcons(Vector3 agentPos, float agentRotY)
        {
            agentIcon.anchoredPosition = WorldToMinimapLocal(agentPos);
            
            // Rotate agent icon to match facing relative to minimap camera
            float mapRotY = minimapCamera.transform.eulerAngles.y;
            agentIcon.localRotation = Quaternion.Euler(0, 0, mapRotY - agentRotY);

            stat1Icon.anchoredPosition = WorldToMinimapLocal(camController.Stationary1WorldPosition);
            stat2Icon.anchoredPosition = WorldToMinimapLocal(camController.Stationary2WorldPosition);
        }

        private Vector2 WorldToMinimapLocal(Vector3 worldPos)
        {
            Vector3 viewportPos = minimapCamera.WorldToViewportPoint(worldPos);
            RectTransform contentRt = contentContainer.GetComponent<RectTransform>();
            return new Vector2(
                (viewportPos.x - 0.5f) * contentRt.rect.width,
                (viewportPos.y - 0.5f) * contentRt.rect.height
            );
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isCollapsed || camController == null) return;

            RectTransform contentRt = contentContainer.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRt, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                Vector2 viewportPos = new Vector2(
                    (localPoint.x / contentRt.rect.width) + 0.5f,
                    (localPoint.y / contentRt.rect.height) + 0.5f
                );

                Vector3 worldPos = minimapCamera.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, minimapCamera.transform.position.y));
                worldPos.y = 10f; // Fixed height for stationary cameras
                
                // Snap to integer grid
                worldPos.x = Mathf.Round(worldPos.x);
                worldPos.z = Mathf.Round(worldPos.z);

                if (CameraController.CurrentMode == CameraMode.Stationary1)
                {
                    camController.Stationary1WorldPosition = worldPos;
                }
                else if (CameraController.CurrentMode == CameraMode.Stationary2)
                {
                    camController.Stationary2WorldPosition = worldPos;
                }
            }
        }
    }
}
#endif