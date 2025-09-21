using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.UI
{
    [System.Serializable]
    public struct ScrollViewOptions
    {
        public string name;
        public bool horizontal;
        public bool vertical;
        public ScrollRect.MovementType movementType;
        public float scrollSensitivity;
        public LayoutElementOptions layoutElement;
        public LayoutGroupOptions contentLayout;
        public ImageOptions backgroundImage;
        public bool showBackground;
        public bool showVerticalScrollbar;
        public bool showHorizontalScrollbar;
        public float scrollbarWidth;
        public Color scrollbarBackgroundColor;
        public Color scrollbarHandleColor;

        public static ScrollViewOptions Vertical(float minHeight = 200f)
        {
            return new ScrollViewOptions
            {
                name = "ScrollView",
                horizontal = false,
                vertical = true,
                movementType = ScrollRect.MovementType.Clamped,
                scrollSensitivity = 10f,
                layoutElement = LayoutElementOptions.MinSize(0, minHeight, 1, 1),
                contentLayout = LayoutGroupOptions.Vertical(),
                backgroundImage = new ImageOptions { Color = new Color(0.1f, 0.1f, 0.1f, 1f) },
                showBackground = true,
                showVerticalScrollbar = true,
                showHorizontalScrollbar = false,
                scrollbarWidth = 20f,
                scrollbarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                scrollbarHandleColor = new Color(0.6f, 0.6f, 0.6f, 1f)
            };
        }

        public static ScrollViewOptions Horizontal(float minWidth = 200f)
        {
            return new ScrollViewOptions
            {
                name = "ScrollView",
                horizontal = true,
                vertical = false,
                movementType = ScrollRect.MovementType.Clamped,
                scrollSensitivity = 10f,
                layoutElement = LayoutElementOptions.MinSize(minWidth, 0, 1, 1),
                contentLayout = LayoutGroupOptions.Horizontal(),
                backgroundImage = new ImageOptions { Color = new Color(0.1f, 0.1f, 0.1f, 1f) },
                showBackground = true,
                showVerticalScrollbar = false,
                showHorizontalScrollbar = true,
                scrollbarWidth = 20f,
                scrollbarBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                scrollbarHandleColor = new Color(0.6f, 0.6f, 0.6f, 1f)
            };
        }
    }

    public static class ScrollViewFactory
    {
        public static ScrollRect CreateScrollView(Transform parent, ScrollViewOptions options)
        {
            var scrollObj = new GameObject(options.name ?? "ScrollView");
            scrollObj.transform.SetParent(parent, false);
            
            // Add layout element for size control
            LayoutFactory.CreateLayoutElement(scrollObj.transform, options.layoutElement);
            
            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = options.horizontal;
            scrollRect.vertical = options.vertical;
            scrollRect.scrollSensitivity = options.scrollSensitivity;
            scrollRect.movementType = options.movementType;
            
            // Add background if needed
            if (options.showBackground)
            {
                ImageFactory.CreateImage(scrollObj.transform, options.backgroundImage);
            }
            
            // Add mask
            var mask = scrollObj.AddComponent<Mask>();
            mask.showMaskGraphic = options.showBackground;
            
            // Create viewport using PanelFactory for consistent setup
            var viewportOptions = PanelOptions.Default(PanelLayoutType.None);
            viewportOptions.Name = "Viewport";
            viewportOptions.HasBackground = false;
            viewportOptions.LayoutElementOptions = LayoutElementOptions.Flexible(1, 1);
            var viewport = PanelFactory.CreatePanel(scrollObj.transform, viewportOptions);
            
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            scrollRect.viewport = viewportRect;
            
            // Create content with layout using PanelFactory
            var contentOptions = PanelOptions.Default();
            contentOptions.Name = "ScrollContent";
            contentOptions.LayoutGroupOptions = options.contentLayout;
            contentOptions.HasBackground = false;
            contentOptions.LayoutElementOptions = LayoutElementOptions.Flexible(1, 1);
            var content = PanelFactory.CreatePanel(viewport.transform, contentOptions);
            
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            
            scrollRect.content = contentRect;
            
            // Add scrollbars if needed
            if (options.showVerticalScrollbar)
            {
                CreateScrollbar(scrollObj, true, options);
            }
            
            if (options.showHorizontalScrollbar)
            {
                CreateScrollbar(scrollObj, false, options);
            }
            
            return scrollRect;
        }

        private static void CreateScrollbar(GameObject scrollView, bool isVertical, ScrollViewOptions options)
        {
            var scrollbarName = isVertical ? "VerticalScrollbar" : "HorizontalScrollbar";
            var scrollbarObj = new GameObject(scrollbarName);
            scrollbarObj.transform.SetParent(scrollView.transform, false);
            
            var scrollbarRect = scrollbarObj.GetComponent<RectTransform>() ?? scrollbarObj.AddComponent<RectTransform>();
            
            if (isVertical)
            {
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.offsetMin = new Vector2(-options.scrollbarWidth, 0);
                scrollbarRect.offsetMax = new Vector2(0, 0);
            }
            else
            {
                scrollbarRect.anchorMin = new Vector2(0, 0);
                scrollbarRect.anchorMax = new Vector2(1, 0);
                scrollbarRect.offsetMin = new Vector2(0, -options.scrollbarWidth);
                scrollbarRect.offsetMax = new Vector2(0, 0);
            }
            
            var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = isVertical ? Scrollbar.Direction.BottomToTop : Scrollbar.Direction.LeftToRight;
            
            // Create scrollbar background using ImageFactory
            var scrollbarImageOptions = new ImageOptions { Color = options.scrollbarBackgroundColor };
            ImageFactory.CreateImage(scrollbarObj.transform, scrollbarImageOptions);
            
            // Create handle area
            var handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(scrollbarObj.transform, false);
            
            var handleAreaRect = handleArea.GetComponent<RectTransform>() ?? handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;
            
            // Create handle using ImageFactory
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            
            var handleImageOptions = new ImageOptions { Color = options.scrollbarHandleColor };
            var handleImage = ImageFactory.CreateImage(handle.transform, handleImageOptions);
            
            scrollbar.handleRect = handle.GetComponent<RectTransform>();
            scrollbar.targetGraphic = handleImage;
            
            // Connect to scroll rect
            var scrollRect = scrollView.GetComponent<ScrollRect>();
            if (isVertical)
            {
                scrollRect.verticalScrollbar = scrollbar;
            }
            else
            {
                scrollRect.horizontalScrollbar = scrollbar;
            }
        }
    }
}