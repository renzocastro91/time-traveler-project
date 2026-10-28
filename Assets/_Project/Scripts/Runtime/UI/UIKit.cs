using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// Fábrica de interfaz construida en código (uGUI). Estilo de ventanas azules con borde claro,
    /// guiño al JRPG clásico, pero con navegación moderna por mando o teclado vía EventSystem.
    /// Cuando llegue el arte definitivo se sustituyen colores/sprites aquí, en un solo lugar.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color WindowColor = new Color(0.07f, 0.11f, 0.32f, 0.94f);
        public static readonly Color WindowBorder = new Color(0.85f, 0.88f, 1f, 0.9f);
        public static readonly Color ButtonColor = new Color(0.12f, 0.18f, 0.45f, 1f);
        public static readonly Color ButtonSelected = new Color(0.95f, 0.78f, 0.25f, 1f);
        public static readonly Color TextColor = new Color(0.97f, 0.97f, 1f);
        public static readonly Color DimText = new Color(0.62f, 0.66f, 0.8f);
        public static readonly Color HpColor = new Color(0.35f, 0.85f, 0.45f);
        public static readonly Color MpColor = new Color(0.35f, 0.6f, 1f);
        public static readonly Color AtbColor = new Color(1f, 0.8f, 0.25f);

        private static Font font;
        public static Font Font
        {
            get { return font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")); }
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            UnityEngine.Object.DontDestroyOnLoad(go);
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>Ventana con fondo y borde.</summary>
        public static RectTransform Window(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var rt = Rect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = WindowColor;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = WindowBorder;
            outline.effectDistance = new Vector2(3, -3);
            return rt;
        }

        public static Text Label(Transform parent, string text, int size = 30, TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = color ?? TextColor;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(2, -2);
            return label;
        }

        public static Text Stretch(Text label, float padding = 16f)
        {
            var rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return label;
        }

        /// <summary>Botón de menú navegable con mando. onSelect se llama al resaltarlo (para descripciones).</summary>
        public static Button Button(Transform parent, string text, Action onClick, Action onSelect = null, bool interactable = true, float height = 52f)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = Color.Lerp(ButtonColor, ButtonSelected, 0.5f);
            colors.selectedColor = ButtonSelected;
            colors.pressedColor = Color.white;
            colors.disabledColor = new Color(0.1f, 0.12f, 0.22f, 1f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            button.interactable = interactable;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;

            var label = Stretch(Label(go.transform, text, 28, TextAnchor.MiddleLeft, interactable ? TextColor : DimText), 14f);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            var hook = go.AddComponent<SelectHook>();
            hook.OnSelected = onSelect;
            hook.Label = label;
            return button;
        }

        /// <summary>Contenedor vertical. Con scroll=true crea un ScrollRect que sigue la selección.</summary>
        public static RectTransform VerticalList(Transform parent, bool scroll, float spacing = 6f)
        {
            RectTransform content;
            if (scroll)
            {
                var viewport = Rect(parent, "Viewport", Vector2.zero, Vector2.one);
                viewport.gameObject.AddComponent<RectMask2D>();
                content = Rect(viewport, "Content", new Vector2(0, 1), new Vector2(1, 1));
                content.pivot = new Vector2(0.5f, 1f);
                var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var sr = viewport.gameObject.AddComponent<ScrollRect>();
                sr.content = content;
                sr.horizontal = false;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.scrollSensitivity = 30f;
            }
            else
            {
                content = Rect(parent, "List", Vector2.zero, Vector2.one);
            }
            var group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(10, 10, 10, 10);
            group.childForceExpandHeight = false;
            group.childControlHeight = true;
            group.childControlWidth = true;
            return content;
        }

        /// <summary>Barra horizontal (vida, PM, tiempo). Devuelve el relleno; se ajusta con SetBar.</summary>
        public static RectTransform Bar(Transform parent, Color color, float height = 10f)
        {
            var back = Rect(parent, "Bar", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, height));
            back.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var layout = back.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            var fill = Rect(back, "Fill", Vector2.zero, Vector2.one);
            fill.gameObject.AddComponent<Image>().color = color;
            return fill;
        }

        public static void SetBar(RectTransform fill, float value)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }

        /// <summary>Selecciona un elemento (para que el mando tenga foco) al final del frame.</summary>
        public static void Focus(Selectable selectable)
        {
            if (selectable == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        public static void FocusFirst(Transform container)
        {
            foreach (Transform child in container)
            {
                var s = child.GetComponent<Selectable>();
                if (s != null && s.interactable && child.gameObject.activeSelf) { Focus(s); return; }
            }
        }
    }

    /// <summary>Notifica la selección de un botón y desplaza su ScrollRect para mantenerlo visible.</summary>
    public sealed class SelectHook : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public Action OnSelected;
        public Text Label;

        public void OnSelect(BaseEventData eventData)
        {
            if (Label != null) Label.color = Color.black;
            OnSelected?.Invoke();
            var scroll = GetComponentInParent<ScrollRect>();
            if (scroll != null) KeepVisible(scroll, (RectTransform)transform);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (Label != null) Label.color = GetComponent<Selectable>().interactable ? UIKit.TextColor : UIKit.DimText;
        }

        private static void KeepVisible(ScrollRect scroll, RectTransform item)
        {
            Canvas.ForceUpdateCanvases();
            var content = scroll.content;
            var viewport = (RectTransform)scroll.transform;
            float viewHeight = viewport.rect.height;
            float contentHeight = content.rect.height;
            if (contentHeight <= viewHeight) return;
            float itemTop = -item.anchoredPosition.y - item.rect.height * (1f - item.pivot.y);
            float itemBottom = itemTop + item.rect.height;
            float scrollTop = content.anchoredPosition.y;
            if (itemTop < scrollTop) scrollTop = itemTop;
            else if (itemBottom > scrollTop + viewHeight) scrollTop = itemBottom - viewHeight;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(scrollTop, 0f, contentHeight - viewHeight));
        }
    }
}
