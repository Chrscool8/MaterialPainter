using Parkitect.UI;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace MaterialPainter2
{
    public class ConstructWindowToggle : MonoBehaviour
    {
        private const string BottomMenuParentName = "BottomMenuParent";
        private const string BottomMenuPath = "BottomMenu";
        private const string ScrollViewPath = "BottomMenu/Scroll View";
        private const string ContentPath = "BottomMenu/Scroll View/Viewport/Content";
        private const string PainterButtonName = "Painter";
        private const string LinebreakName = "linebreak";
        private const string ClockbarBackgroundName = "Clockbar_BG";

        private static readonly string[] DirectRightButtonNames =
        {
            "ScenarioEditor",
            "CustomCampaignMapEditor",
            "ScenarioMarkerBuilder"
        };

        private GameObject mp2Button;
        private GameObject mp2Linebreak;
        private GameObject windowTemplateParent;
        private Window_Main windowTemplate;

        private Coroutine installCoroutine;
        private bool installed;
        private bool installAborted;

        private RectTransform bottomMenuParentRect;
        private RectTransform bottomMenuRect;
        private RectTransform scrollViewRect;
        private RectTransform viewportRect;
        private RectTransform contentRect;
        private LayoutElement bottomMenuLayout;
        private readonly Dictionary<LayoutElement, float> originalPreferredWidths = new Dictionary<LayoutElement, float>();

        private Vector2 originalContentSizeDelta;
        private float originalBottomMenuPreferredWidth = -1f;
        private float originalBottomMenuWidth;
        private float originalScrollViewWidth;
        private float originalViewportWidth;
        private RectTransform clockbarBackgroundRect;
        private Vector2 originalClockbarBackgroundSizeDelta;
        private bool hasClockbarBackgroundOriginal;
        private readonly Dictionary<RectTransform, Vector2> originalRightButtonPositions = new Dictionary<RectTransform, Vector2>();

        private float buttonStep;
        private float linebreakOffsetFromPrevious;
        private float buttonOffsetAfterLinebreak;
        private float lastButtonX;
        private float buttonY;
        private Vector2 buttonSize;
        private float linebreakY;
        private Vector2 linebreakSize;

        private int lastScreenWidth;
        private int lastScreenHeight;
        private float lastUiScale;

        private object uiScaleEventSource;
        private EventInfo uiScaleEventInfo;
        private Delegate uiScaleDelegate;

        public void OnEnable()
        {
            installAborted = false;
            CacheScreenAndScale();
            TrySubscribeToUiScaleChanged();
            installCoroutine = StartCoroutine(InstallWhenReady());
        }

        public void Update()
        {
            if (!installed)
            {
                if (!installAborted && installCoroutine == null)
                    installCoroutine = StartCoroutine(InstallWhenReady());

                return;
            }

            if (!InstalledObjectsAreAlive())
            {
                ResetAfterMenuDestroyed();
                installCoroutine = StartCoroutine(InstallWhenReady());
                return;
            }

            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height || !Mathf.Approximately(lastUiScale, GetUiScale()))
            {
                CacheScreenAndScale();
                ApplyLayout();
            }
        }

        public void OnDisable()
        {
            if (installCoroutine != null)
            {
                StopCoroutine(installCoroutine);
                installCoroutine = null;
            }

            TryUnsubscribeFromUiScaleChanged();
            CloseOpenMaterialPainterWindow();
            RestoreLayout();
            DestroyCreatedObjects();

            installed = false;
            installAborted = false;
            contentRect = null;
            bottomMenuRect = null;
            scrollViewRect = null;
            viewportRect = null;
            bottomMenuParentRect = null;
            bottomMenuLayout = null;
            clockbarBackgroundRect = null;
            hasClockbarBackgroundOriginal = false;
            originalRightButtonPositions.Clear();
            originalPreferredWidths.Clear();
        }

        private IEnumerator InstallWhenReady()
        {
            while (!installed && !installAborted)
            {
                if (TryInstall())
                {
                    installCoroutine = null;
                    yield break;
                }

                yield return null;
            }

            installCoroutine = null;
        }

        private bool TryInstall()
        {
            GameObject bottomMenuParentGo = GameObject.Find(BottomMenuParentName);
            if (bottomMenuParentGo == null)
                return false;

            Transform bottomMenuParent = bottomMenuParentGo.transform;
            Transform bottomMenu = bottomMenuParent.Find(BottomMenuPath);
            Transform scrollView = bottomMenuParent.Find(ScrollViewPath);
            Transform content = bottomMenuParent.Find(ContentPath);

            if (bottomMenu == null || scrollView == null || content == null)
                return false;

            ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
            if (scrollRect == null)
                return false;

            if (!scrollRect.horizontal || scrollRect.vertical)
            {
                MP2.MPDebug("Material Painter bottom menu insertion skipped: expected horizontal bottom menu ScrollRect.", always_show: true);
                installAborted = true;
                return true;
            }

            Transform painterTransform = content.Find(PainterButtonName);
            Transform linebreakTransform = content.Find(LinebreakName);
            if (painterTransform == null || linebreakTransform == null)
                return false;

            if (content.Find(MP2.MOD_DISPLAY_NAME) != null)
            {
                MP2.MPDebug("Material Painter bottom menu insertion skipped: button already exists.", always_show: true);
                installAborted = true;
                return true;
            }

            contentRect = content.GetComponent<RectTransform>();
            bottomMenuRect = bottomMenu.GetComponent<RectTransform>();
            scrollViewRect = scrollView.GetComponent<RectTransform>();
            viewportRect = scrollRect.viewport != null ? scrollRect.viewport : content.parent.GetComponent<RectTransform>();
            bottomMenuParentRect = bottomMenuParent.GetComponent<RectTransform>();
            bottomMenuLayout = bottomMenu.GetComponent<LayoutElement>();

            if (contentRect == null || bottomMenuRect == null || scrollViewRect == null || viewportRect == null || bottomMenuParentRect == null)
                return false;

            RebuildBottomMenuLayout();

            if (!CaptureLayoutMetrics(content, linebreakTransform.GetComponent<RectTransform>()))
            {
                MP2.MPDebug("Material Painter bottom menu insertion skipped: could not infer native button spacing.", always_show: true);
                installAborted = true;
                return true;
            }

            CaptureOriginalLayout(bottomMenu, contentRect);

            windowTemplate = CreateWindowTemplate();
            if (windowTemplate == null)
            {
                MP2.MPDebug("Material Painter bottom menu insertion skipped: could not create window template.", always_show: true);
                installAborted = true;
                return true;
            }

            if (!CreateNativeMenuObjects(content, painterTransform.gameObject, linebreakTransform.gameObject))
            {
                MP2.MPDebug("Material Painter bottom menu insertion skipped: could not create native button clone.", always_show: true);
                DestroyCreatedObjects();
                installAborted = true;
                return true;
            }

            ApplyLayout();
            mp2Linebreak.SetActive(true);
            mp2Button.SetActive(true);
            ApplyLayout();

            installed = true;
            CacheScreenAndScale();
            TrySubscribeToUiScaleChanged();
            MP2.MPDebug("Material Painter bottom menu button installed.");
            return true;
        }

        private bool CaptureLayoutMetrics(Transform content, RectTransform linebreakRect)
        {
            if (linebreakRect == null)
                return false;

            List<RectTransform> buttonRects = new List<RectTransform>();
            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (child == null || child.GetComponent<UIMenuButton>() == null)
                    continue;

                RectTransform rect = child.GetComponent<RectTransform>();
                if (rect != null)
                    buttonRects.Add(rect);
            }

            if (buttonRects.Count < 2)
                return false;

            buttonRects.Sort((left, right) => left.anchoredPosition.x.CompareTo(right.anchoredPosition.x));

            List<float> gaps = new List<float>();
            for (int i = 1; i < buttonRects.Count; i++)
            {
                float gap = buttonRects[i].anchoredPosition.x - buttonRects[i - 1].anchoredPosition.x;
                if (gap > 0f)
                    gaps.Add(gap);
            }

            if (gaps.Count == 0)
                return false;

            buttonStep = Median(gaps);
            if (buttonStep <= 0f)
                return false;

            RectTransform lastButton = buttonRects[buttonRects.Count - 1];
            lastButtonX = lastButton.anchoredPosition.x;
            buttonY = lastButton.anchoredPosition.y;
            buttonSize = lastButton.sizeDelta;

            linebreakY = linebreakRect.anchoredPosition.y;
            linebreakSize = linebreakRect.sizeDelta;

            RectTransform previousBeforeLinebreak = null;
            RectTransform nextAfterLinebreak = null;
            for (int i = 0; i < buttonRects.Count; i++)
            {
                if (buttonRects[i].anchoredPosition.x < linebreakRect.anchoredPosition.x)
                {
                    previousBeforeLinebreak = buttonRects[i];
                    continue;
                }

                if (buttonRects[i].anchoredPosition.x > linebreakRect.anchoredPosition.x)
                {
                    nextAfterLinebreak = buttonRects[i];
                    break;
                }
            }

            if (previousBeforeLinebreak != null && nextAfterLinebreak != null)
            {
                linebreakOffsetFromPrevious = linebreakRect.anchoredPosition.x - previousBeforeLinebreak.anchoredPosition.x;
                buttonOffsetAfterLinebreak = nextAfterLinebreak.anchoredPosition.x - previousBeforeLinebreak.anchoredPosition.x;
            }

            if (linebreakOffsetFromPrevious <= 0f || linebreakOffsetFromPrevious >= buttonOffsetAfterLinebreak)
                linebreakOffsetFromPrevious = buttonStep * 0.55f;

            if (buttonOffsetAfterLinebreak <= 0f)
                buttonOffsetAfterLinebreak = buttonStep;

            return true;
        }

        private void CaptureOriginalLayout(Transform bottomMenu, RectTransform content)
        {
            originalContentSizeDelta = content.sizeDelta;
            originalBottomMenuPreferredWidth = bottomMenuLayout != null ? bottomMenuLayout.preferredWidth : -1f;
            originalBottomMenuWidth = ResolveWidth(bottomMenuRect, originalBottomMenuPreferredWidth >= 0f ? originalBottomMenuPreferredWidth : originalContentSizeDelta.x);
            originalScrollViewWidth = ResolveWidth(scrollViewRect, originalContentSizeDelta.x);
            originalViewportWidth = ResolveWidth(viewportRect, originalScrollViewWidth);
            originalRightButtonPositions.Clear();
            originalPreferredWidths.Clear();
            CaptureClockbarBackground();

            CapturePreferredWidth(bottomMenuLayout);
            CapturePreferredWidth(scrollViewRect.GetComponent<LayoutElement>());
            CapturePreferredWidth(viewportRect.GetComponent<LayoutElement>());
            CapturePreferredWidth(contentRect.GetComponent<LayoutElement>());

            foreach (string buttonName in DirectRightButtonNames)
            {
                Transform rightButton = bottomMenu.Find(buttonName);
                if (rightButton == null)
                    continue;

                RectTransform rect = rightButton.GetComponent<RectTransform>();
                if (rect != null && !originalRightButtonPositions.ContainsKey(rect))
                    originalRightButtonPositions.Add(rect, rect.anchoredPosition);
            }
        }

        private Window_Main CreateWindowTemplate()
        {
            windowTemplateParent = new GameObject("Material Painter Window Template Parent");
            windowTemplateParent.transform.SetParent(transform, false);
            windowTemplateParent.SetActive(false);

            GameObject windowGo = new GameObject(MP2.Instance.getName());
            windowGo.transform.SetParent(windowTemplateParent.transform, false);
            windowGo.SetActive(true);

            RectTransform rect = windowGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 150f);

            Window_Main window = windowGo.AddComponent<Window_Main>();
            UIWindowSettings windowSettings = windowGo.AddComponent<UIWindowSettings>();
            windowSettings.closable = true;
            windowSettings.pinnable = true;
            windowSettings.destroyWhenClosed = true;
            windowSettings.defaultWindowPosition = new Vector2(0.5f, 0.5f);
            windowSettings.title = MP2.MOD_DISPLAY_NAME;
            windowSettings.uniqueTag = MP2.Instance.getName();
            windowSettings.uniqueTagString = MP2.Instance.getName();
            windowSettings.resizeability = 0;
            windowSettings.mask = 0;
            windowSettings.isBuilderWindow = false;
            windowSettings.spawnLocation = UIWindowSettings.SpawnLocation.BottomMenu;

            return window;
        }

        private bool CreateNativeMenuObjects(Transform content, GameObject painterButton, GameObject linebreak)
        {
            bool linebreakWasActive = linebreak.activeSelf;
            linebreak.SetActive(false);
            try
            {
                mp2Linebreak = Instantiate(linebreak);
            }
            finally
            {
                linebreak.SetActive(linebreakWasActive);
            }

            mp2Linebreak.name = "linebreak " + MP2.MOD_DISPLAY_NAME;
            mp2Linebreak.transform.SetParent(content, false);

            bool painterWasActive = painterButton.activeSelf;
            painterButton.SetActive(false);
            try
            {
                mp2Button = Instantiate(painterButton);
            }
            finally
            {
                painterButton.SetActive(painterWasActive);
            }

            mp2Button.name = MP2.MOD_DISPLAY_NAME;
            mp2Button.transform.SetParent(content, false);
            mp2Button.transform.SetAsLastSibling();
            mp2Linebreak.transform.SetAsLastSibling();

            UIMenuWindowButton windowButton = mp2Button.GetComponent<UIMenuWindowButton>();
            if (windowButton == null)
                return false;

            windowButton.hotkeyIdentifier = string.Empty;
            windowButton.windowContentGO = windowTemplate;

            UITooltip tooltip = mp2Button.GetComponent<UITooltip>();
            if (tooltip != null)
                tooltip.text = MP2.MOD_DISPLAY_NAME;

            Toggle toggle = mp2Button.GetComponent<Toggle>();
            if (toggle != null)
                toggle.isOn = false;

            Transform iconTransform = mp2Button.transform.Find("Image");
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon != null)
                icon.sprite = MP2.get_sprite("icon_magic_brush");

            return true;
        }

        private void ApplyLayout()
        {
            if (contentRect == null)
                return;

            float delta = GetLayoutDelta();

            contentRect.sizeDelta = new Vector2(originalContentSizeDelta.x + delta, originalContentSizeDelta.y);
            if (bottomMenuLayout != null)
            {
                float preferredWidth = originalBottomMenuPreferredWidth >= 0f ? originalBottomMenuPreferredWidth : originalContentSizeDelta.x;
                bottomMenuLayout.preferredWidth = preferredWidth + delta;
            }
            ApplyPreferredWidthDelta(delta);
            SetWidth(bottomMenuRect, originalBottomMenuWidth + delta);
            SetWidth(scrollViewRect, originalScrollViewWidth + delta);
            SetWidth(viewportRect, originalViewportWidth + delta);
            ApplyClockbarBackgroundDelta(delta);

            foreach (KeyValuePair<RectTransform, Vector2> pair in originalRightButtonPositions)
            {
                if (pair.Key != null)
                    pair.Key.anchoredPosition = new Vector2(pair.Value.x + delta, pair.Value.y);
            }

            RectTransform linebreakRect = mp2Linebreak != null ? mp2Linebreak.GetComponent<RectTransform>() : null;
            if (linebreakRect != null)
            {
                linebreakRect.anchoredPosition = new Vector2(lastButtonX + linebreakOffsetFromPrevious, linebreakY);
                linebreakRect.sizeDelta = linebreakSize;
            }

            RectTransform buttonRect = mp2Button != null ? mp2Button.GetComponent<RectTransform>() : null;
            if (buttonRect != null)
            {
                buttonRect.anchoredPosition = new Vector2(lastButtonX + delta, buttonY);
                buttonRect.sizeDelta = buttonSize;
            }

            if (mp2Button != null)
                mp2Button.transform.SetAsLastSibling();

            if (mp2Linebreak != null)
                mp2Linebreak.transform.SetAsLastSibling();

            RebuildBottomMenuLayout();
            RefreshGraphics(bottomMenuRect);
            RefreshGraphics(clockbarBackgroundRect);
        }

        private void RestoreLayout()
        {
            if (contentRect != null)
                contentRect.sizeDelta = originalContentSizeDelta;

            if (bottomMenuLayout != null)
                bottomMenuLayout.preferredWidth = originalBottomMenuPreferredWidth;

            RestorePreferredWidths();
            SetWidth(bottomMenuRect, originalBottomMenuWidth);
            SetWidth(scrollViewRect, originalScrollViewWidth);
            SetWidth(viewportRect, originalViewportWidth);
            RestoreClockbarBackground();

            foreach (KeyValuePair<RectTransform, Vector2> pair in originalRightButtonPositions)
            {
                if (pair.Key != null)
                    pair.Key.anchoredPosition = pair.Value;
            }

            RebuildBottomMenuLayout();
            RefreshGraphics(bottomMenuRect);
            RefreshGraphics(clockbarBackgroundRect);
        }

        private void RebuildBottomMenuLayout()
        {
            if (contentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            if (viewportRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);

            if (scrollViewRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewRect);

            if (bottomMenuRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(bottomMenuRect);

            if (bottomMenuParentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(bottomMenuParentRect);

            Canvas.ForceUpdateCanvases();
        }

        private void CloseOpenMaterialPainterWindow()
        {
            if (Window_Main.Instance != null && Window_Main.Instance.windowFrame != null)
                Window_Main.Instance.windowFrame.close();
        }

        private void DestroyCreatedObjects()
        {
            if (mp2Button != null)
                DestroyImmediate(mp2Button);

            if (mp2Linebreak != null)
                DestroyImmediate(mp2Linebreak);

            if (windowTemplateParent != null)
                DestroyImmediate(windowTemplateParent);

            mp2Button = null;
            mp2Linebreak = null;
            windowTemplateParent = null;
            windowTemplate = null;
        }

        private bool InstalledObjectsAreAlive()
        {
            return mp2Button != null
                && mp2Linebreak != null
                && windowTemplateParent != null
                && contentRect != null
                && bottomMenuRect != null
                && scrollViewRect != null
                && viewportRect != null
                && bottomMenuParentRect != null
                && (!hasClockbarBackgroundOriginal || clockbarBackgroundRect != null);
        }

        private void ResetAfterMenuDestroyed()
        {
            MP2.MPDebug("Material Painter bottom menu was destroyed; waiting for rebuilt menu.");

            installed = false;
            installAborted = false;
            contentRect = null;
            bottomMenuRect = null;
            scrollViewRect = null;
            viewportRect = null;
            bottomMenuParentRect = null;
            bottomMenuLayout = null;
            clockbarBackgroundRect = null;
            hasClockbarBackgroundOriginal = false;
            originalRightButtonPositions.Clear();
            originalPreferredWidths.Clear();

            DestroyCreatedObjects();
        }

        private void CacheScreenAndScale()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastUiScale = GetUiScale();
        }

        private float GetUiScale()
        {
            return Settings.Instance != null ? Settings.Instance.uiScale : 1f;
        }

        private float GetLayoutDelta()
        {
            return buttonOffsetAfterLinebreak > 0f ? buttonOffsetAfterLinebreak : buttonStep;
        }

        private void CaptureClockbarBackground()
        {
            clockbarBackgroundRect = null;
            hasClockbarBackgroundOriginal = false;

            GameObject clockbarBackground = GameObject.Find(ClockbarBackgroundName);
            if (clockbarBackground == null)
            {
                MP2.MPDebug("Material Painter bottom menu background extension skipped: Clockbar_BG was not found.", always_show: true);
                return;
            }

            clockbarBackgroundRect = clockbarBackground.GetComponent<RectTransform>();
            if (clockbarBackgroundRect == null)
            {
                MP2.MPDebug("Material Painter bottom menu background extension skipped: Clockbar_BG has no RectTransform.", always_show: true);
                return;
            }

            originalClockbarBackgroundSizeDelta = clockbarBackgroundRect.sizeDelta;
            hasClockbarBackgroundOriginal = true;
        }

        private void ApplyClockbarBackgroundDelta(float delta)
        {
            if (!hasClockbarBackgroundOriginal || clockbarBackgroundRect == null)
                return;

            float width = originalClockbarBackgroundSizeDelta.x + delta;
            clockbarBackgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            clockbarBackgroundRect.sizeDelta = new Vector2(width, originalClockbarBackgroundSizeDelta.y);
        }

        private void RestoreClockbarBackground()
        {
            if (!hasClockbarBackgroundOriginal || clockbarBackgroundRect == null)
                return;

            clockbarBackgroundRect.sizeDelta = originalClockbarBackgroundSizeDelta;
        }

        private void CapturePreferredWidth(LayoutElement layoutElement)
        {
            if (layoutElement != null && layoutElement.preferredWidth >= 0f && !originalPreferredWidths.ContainsKey(layoutElement))
                originalPreferredWidths.Add(layoutElement, layoutElement.preferredWidth);
        }

        private void ApplyPreferredWidthDelta(float delta)
        {
            foreach (KeyValuePair<LayoutElement, float> pair in originalPreferredWidths)
            {
                if (pair.Key != null)
                    pair.Key.preferredWidth = pair.Value + delta;
            }
        }

        private void RestorePreferredWidths()
        {
            foreach (KeyValuePair<LayoutElement, float> pair in originalPreferredWidths)
            {
                if (pair.Key != null)
                    pair.Key.preferredWidth = pair.Value;
            }
        }

        private static float ResolveWidth(RectTransform rectTransform, float fallback)
        {
            if (rectTransform == null)
                return fallback;

            float width = rectTransform.rect.width;
            return width > 0f ? width : fallback;
        }

        private static void SetWidth(RectTransform rectTransform, float width)
        {
            if (rectTransform != null && width > 0f)
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private static void RefreshGraphics(RectTransform root)
        {
            if (root == null)
                return;

            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].SetLayoutDirty();
                graphics[i].SetVerticesDirty();
                graphics[i].SetMaterialDirty();
            }
        }

        private void OnUiScaleChanged()
        {
            CacheScreenAndScale();
            ApplyLayout();
        }

        private void TrySubscribeToUiScaleChanged()
        {
            if (uiScaleDelegate != null)
                return;

            try
            {
                Type eventManagerType = typeof(GameController).Assembly.GetType("InternalEventManager");
                if (eventManagerType == null)
                    return;

                PropertyInfo existsProperty = eventManagerType.GetProperty("Exists", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (existsProperty != null && existsProperty.PropertyType == typeof(bool) && !(bool)existsProperty.GetValue(null, null))
                    return;

                PropertyInfo instanceProperty = eventManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                uiScaleEventSource = instanceProperty != null ? instanceProperty.GetValue(null, null) : null;
                if (uiScaleEventSource == null)
                    return;

                uiScaleEventInfo = eventManagerType.GetEvent("OnUIScaleChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (uiScaleEventInfo == null)
                    return;

                MethodInfo handlerMethod = GetType().GetMethod(nameof(OnUiScaleChanged), BindingFlags.Instance | BindingFlags.NonPublic);
                uiScaleDelegate = Delegate.CreateDelegate(uiScaleEventInfo.EventHandlerType, this, handlerMethod);
                uiScaleEventInfo.AddEventHandler(uiScaleEventSource, uiScaleDelegate);
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Could not subscribe to UI scale changes: " + ex.Message);
                uiScaleEventSource = null;
                uiScaleEventInfo = null;
                uiScaleDelegate = null;
            }
        }

        private void TryUnsubscribeFromUiScaleChanged()
        {
            if (uiScaleEventSource == null || uiScaleEventInfo == null || uiScaleDelegate == null)
                return;

            try
            {
                uiScaleEventInfo.RemoveEventHandler(uiScaleEventSource, uiScaleDelegate);
            }
            catch (Exception ex)
            {
                MP2.MPDebug("Could not unsubscribe from UI scale changes: " + ex.Message);
            }

            uiScaleEventSource = null;
            uiScaleEventInfo = null;
            uiScaleDelegate = null;
        }

        private static float Median(List<float> values)
        {
            values.Sort();
            int middle = values.Count / 2;

            if (values.Count % 2 == 1)
                return values[middle];

            return (values[middle - 1] + values[middle]) / 2f;
        }
    }
}
