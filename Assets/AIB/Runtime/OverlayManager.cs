using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AIB.Runtime
{
    public class OverlayManager : MonoBehaviour
    {
        [Serializable]
        public class OverlayGroup
        {
            public string name;
            public GameObject rootObject;
            public bool enabledByDefault = true;
            [HideInInspector] public CanvasGroup canvasGroup;
        }

        [SerializeField] private List<OverlayGroup> overlays = new List<OverlayGroup>();
        [SerializeField] private float defaultAlpha = 0.85f;

        private void Start()
        {
            foreach (OverlayGroup group in overlays)
            {
                if (group.rootObject == null) continue;

                group.canvasGroup = group.rootObject.GetComponent<CanvasGroup>();
                if (group.canvasGroup == null)
                    group.canvasGroup = group.rootObject.AddComponent<CanvasGroup>();

                group.canvasGroup.alpha = group.enabledByDefault ? defaultAlpha : 0f;
                group.rootObject.SetActive(group.enabledByDefault);
            }
        }

        public void ToggleOverlay(string name)
        {
            OverlayGroup group = FindGroup(name);
            if (group == null || group.rootObject == null) return;

            bool active = !group.rootObject.activeSelf;
            group.rootObject.SetActive(active);
            if (group.canvasGroup != null)
                group.canvasGroup.alpha = active ? defaultAlpha : 0f;
        }

        public void ShowAll()
        {
            foreach (OverlayGroup group in overlays)
            {
                if (group.rootObject == null) continue;
                group.rootObject.SetActive(true);
                if (group.canvasGroup != null)
                    group.canvasGroup.alpha = defaultAlpha;
            }
        }

        public void HideAll()
        {
            foreach (OverlayGroup group in overlays)
            {
                if (group.rootObject == null) continue;
                group.rootObject.SetActive(false);
            }
        }

        public void SetAlpha(float alpha)
        {
            defaultAlpha = Mathf.Clamp01(alpha);
            foreach (OverlayGroup group in overlays)
            {
                if (group.rootObject != null && group.rootObject.activeSelf && group.canvasGroup != null)
                    group.canvasGroup.alpha = defaultAlpha;
            }
        }

        public int OverlayCount => overlays.Count;
        public string GetOverlayName(int index) => index >= 0 && index < overlays.Count ? overlays[index].name : "";
        public bool IsOverlayActive(int index) => index >= 0 && index < overlays.Count && overlays[index].rootObject != null && overlays[index].rootObject.activeSelf;

        private OverlayGroup FindGroup(string name)
        {
            foreach (OverlayGroup group in overlays)
            {
                if (string.Equals(group.name, name, StringComparison.OrdinalIgnoreCase))
                    return group;
            }
            return null;
        }
    }
}
