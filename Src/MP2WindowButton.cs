using Parkitect.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialPainter2
{
    public class MP2WindowButton : UIMenuButton, IPointerClickHandler
    {
        public static MP2WindowButton Instance { get; private set; }
        public static MP2Window Window { get; private set; }

        private Toggle toggle;

        protected override void Awake()
        {
            base.Awake();

            Instance = this;

            var tooltip = gameObject.GetComponent<UITooltip>();
            tooltip.text = gameObject.name;

            toggle = gameObject.GetComponent<Toggle>();
            var colors = toggle.colors;
            colors.normalColor = new Color(0.792f, 0.805f, 1f, 1f);
            colors.highlightedColor = Color.white;
            toggle.colors = colors;

            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(387 + 10, -15);
        }

        public void SetButtonEnabled(bool state)
        {
            MP2.MPDebug("SetButtonEnabled = " + state);
        }

        public void SetWindowOpened(bool state)
        {
            MP2.MPDebug("SetWindowOpened = " + state);
            toggle.isOn = state;
        }

        protected override void onSelected()
        {
            var prefab = ConstructWindowPrefab();
            if (prefab == null)
                MP2.MPDebug("Window prefab is null");

            if (ScriptableSingleton<UIAssetManager>.Instance.uiWindowFrameGO == null)
                MP2.MPDebug("uiWindowFrameGO is null, what?");

            this.windowInstance = UIWindowsController.Instance.spawnWindow(prefab, null);
            this.windowInstance.OnClose += this.OnWindowClose;
        }

        protected override void onDeselected()
        {
            if (this.windowInstance != null)
            {
                this.windowInstance.close();
                this.windowInstance = null;
            }
        }

        public MP2Window ConstructWindowPrefab()
        {
            MP2.MPDebug("ConstructWindowPrefab");
            var WindowPrefab = new GameObject(MP2.Instance.getName());
            WindowPrefab.SetActive(false);

            var rect = WindowPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(195, 88 + 40);
            WindowPrefab.AddComponent<CanvasRenderer>();
            Window = WindowPrefab.AddComponent<MP2Window>();

            var windowSettings = WindowPrefab.AddComponent<UIWindowSettings>();
            windowSettings.closable = true;
            windowSettings.defaultWindowPosition = new Vector2(0.5f, 0.5f);
            windowSettings.title = "Material Painter";
            windowSettings.uniqueTag = MP2.Instance.getName();
            windowSettings.uniqueTagString = MP2.Instance.getName();
            windowSettings.resizeability = 0;

            WindowPrefab.SetActive(true);

            MP2.controller.ActivatePipe();

            return Window;
        }

        private void OnWindowClose(UIWindowFrame window)
        {
            setSelected(false);
            MP2.controller.DeactivatePipe();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }

        private UIWindowFrame windowInstance;
    }
}