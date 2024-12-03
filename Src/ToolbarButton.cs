using Parkitect.UI;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialPainter2
{
    public class ConstructWindowToggle : MonoBehaviour
    {
        private GameObject mp2_button;

        public ConstructWindowToggle()
        {
        }

        public static void RemoveListeners(Toggle tgl)
        {
            tgl.onValueChanged.RemoveAllListeners();
            int persistentEventCount = tgl.onValueChanged.GetPersistentEventCount();
            for (int index = 0; index < persistentEventCount; ++index)
                tgl.onValueChanged.SetPersistentListenerState(index, UnityEventCallState.Off);
        }

        public void OnEnable()
        {
            //Utils.PrintHierarchy(GameObject.Find("UIWindowsController"), 1, listComponents: true);

            Transform content = GameObject.Find("BottomMenuParent").transform.Find("BottomMenu/Scroll View/Viewport/Content");
            Transform viewport = GameObject.Find("BottomMenuParent").transform.Find("BottomMenu/Scroll View/Viewport");
            Transform scroll_view = GameObject.Find("BottomMenuParent").transform.Find("BottomMenu/Scroll View");
            ScrollRect scroll_rect = GameObject.Find("BottomMenuParent").transform.Find("BottomMenu/Scroll View").GetComponent<ScrollRect>();
            //MP2.MPDebug($"!!!!!{content.gameObject}");

            GameObject line_break1 = GameObject.Find("linebreak");
            line_break1.SetActive(false);
            //GameObject line_break2 = GameObject.Instantiate(line_break1);
            //line_break2.transform.SetParent(content, false);

            float line_break_width = line_break1.GetComponent<RectTransform>().sizeDelta.x;

            line_break1.SetActive(true);
            //line_break2.SetActive(true);

            var painterButton = GameObject.Find("Painter");
            painterButton.SetActive(false);

            Vector2 button_width = (GameObject.Find("PathBuilder").GetComponent<RectTransform>().anchoredPosition - GameObject.Find("DecoBuilder").GetComponent<RectTransform>().anchoredPosition);
            MP2.MPDebug($"!!!!! {button_width} {line_break_width}");

            mp2_button = GameObject.Instantiate(painterButton);
            mp2_button.transform.SetParent(content, false);
            mp2_button.name = "Material Painter";


            Image ic = mp2_button.transform.Find("Image").GetComponent<Image>();
            ic.sprite = MP2.get_sprite("icon_magic_brush");

            //Utils.PrintHierarchy(GameObject.Find("MenuCanvas"), 1, listComponents: true);

            //line_break2.GetComponent<RectTransform>().anchoredPosition += button_width * 2.25f;
            //mp2_button.GetComponent<RectTransform>().anchoredPosition += button_width * 4.25f;

            RectTransform vrt = viewport.GetComponent<RectTransform>();
            vrt.sizeDelta = new Vector2((float)(vrt.sizeDelta.x + (button_width[0] * 5f)), vrt.sizeDelta.y);

            RectTransform crt = content.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2((float)(crt.sizeDelta.x + (button_width[0] * 5f)), crt.sizeDelta.y);

            //RectTransform srt = scroll_view.GetComponent<RectTransform>();
            //srt.sizeDelta = new Vector2((float)(srt.sizeDelta.x + (button_width[0] * 3)), srt.sizeDelta.y);

            //scroll_rect.content.sizeDelta = new Vector2((float)(scroll_rect.content.sizeDelta.x + (button_width[0] * 3f)), scroll_rect.content.sizeDelta.y);
            //scroll_rect.viewport.sizeDelta = new Vector2((float)(scroll_rect.viewport.sizeDelta.x + (button_width[0] * 3f)), scroll_rect.viewport.sizeDelta.y);

            /*
            RectTransform viewport = mpButtonGo.transform.parent.GetComponent<RectTransform>();
            viewport.sizeDelta = new Vector2(viewport.sizeDelta.x + 40 + 30, viewport.sizeDelta.y);

            RectTransform rt = mpButtonGo.GetComponent<RectTransform>();
            MP2.MPDebug(rt.rect.ToString());
            */

            //Features setup
            DestroyImmediate(mp2_button.GetComponent<UIMenuWindowButton>());
            var mpWindowButton = mp2_button.AddComponent<ToolbarButton>();
            //mpWindowButton.hotkeyIdentifier = MaterialPainter2.MPActiveHotkey.Identifier;
            var toggle = mp2_button.GetComponent<Toggle>();
            RemoveListeners(toggle);
            toggle.onValueChanged.AddListener(x => mpWindowButton.onChanged());

            painterButton.SetActive(true);
            mp2_button.SetActive(true);

            mp2_button.transform.position += new Vector3(button_width.x * 1.5f, 0, 0);

            if (GameController.Instance.isInScenarioEditor)
                mp2_button.transform.position += new Vector3(button_width.x * 2.15f, 0, 0);
        }

        public void OnDisable()
        {
            DestroyImmediate(mp2_button);
        }
    }

    public class ToolbarButton : UIMenuButton, IPointerClickHandler
    {
        public static ToolbarButton Instance { get; private set; }
        public static Window_Main Window { get; private set; }

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

            windowInstance = UIWindowsController.Instance.spawnWindow(prefab, null);
            windowInstance.OnClose += this.OnWindowClose;
        }

        protected override void onDeselected()
        {
            if (windowInstance != null)
            {
                this.windowInstance.close();
                windowInstance = null;
            }
        }

        public Window_Main ConstructWindowPrefab()
        {
            MP2.MPDebug("ConstructWindowPrefab");
            var WindowPrefab = new GameObject(MP2.Instance.getName());
            WindowPrefab.SetActive(false);

            var rect = WindowPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 150);

            //WindowPrefab.AddComponent<CanvasRenderer>();
            Window = WindowPrefab.AddComponent<Window_Main>();
            //WindowPrefab.AddComponent<Canvas>();

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