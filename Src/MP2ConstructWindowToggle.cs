using Parkitect.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialPainter2
{
    public class ConstructWindowToggle : MonoBehaviour
    {
        private GameObject menuCanvasRoot;
        private GameObject mpButtonGo;

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
            MP2.MPDebug("Construct Applied!");
            menuCanvasRoot = GameObject.Find("MenuCanvas");

            var painterButton = GameObject.Find("Painter");

            painterButton.SetActive(false);

            mpButtonGo = GameObject.Instantiate(painterButton, painterButton.transform.parent);
            mpButtonGo.name = "Material Painter";

            MP2.MPDebug(painterButton.transform.parent.ToString());
            (mpButtonGo.transform.parent.parent.parent.transform as RectTransform).sizeDelta += new Vector2(80, 0);
            (mpButtonGo.transform.parent.parent.parent.transform as RectTransform).Translate(50f, 0, 0);

            //PrintHierarchy.PrintHierarchyRecursive(GameObject.Find("ScenarioEditor").transform, 1, GameObject.Find("ScenarioEditor").transform);

            RectTransform viewport = mpButtonGo.transform.parent.GetComponent<RectTransform>();
            viewport.sizeDelta = new Vector2(viewport.sizeDelta.x + 40 + 30, viewport.sizeDelta.y);

            Image ic = mpButtonGo.transform.Find("Image").GetComponent<Image>();
            MP2.MPDebug(ic.ToString());
            ic.sprite = MP2.get_sprite("icon_magic_brush");

            RectTransform rt = mpButtonGo.GetComponent<RectTransform>();
            MP2.MPDebug(rt.rect.ToString());

            //Features setup
            GameObject.DestroyImmediate(mpButtonGo.GetComponent<UIMenuWindowButton>());
            var mpWindowButton = mpButtonGo.AddComponent<MP2WindowButton>();
            //mpWindowButton.hotkeyIdentifier = MaterialPainter2.MPActiveHotkey.Identifier;
            var toggle = mpButtonGo.GetComponent<Toggle>();
            RemoveListeners(toggle);
            toggle.onValueChanged.AddListener(x => mpWindowButton.onChanged());

            painterButton.SetActive(true);
            mpButtonGo.SetActive(true);
        }

        public void OnDisable()
        {
            GameObject.DestroyImmediate(mpButtonGo);
        }
    }
}