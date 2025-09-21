using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.UI
{
    public enum PanelLayoutType
    {
        None,
        Horizontal,
        Vertical,
        Grid
    }
    [System.Serializable]
    public struct PanelOptions
    {
        public string Name;
        public LayoutGroupOptions LayoutGroupOptions;
        public LayoutElementOptions LayoutElementOptions;
        public ImageOptions ImageOptions;
        public bool HasBackground;
        public bool HasOutline;
        public Color OutlineColor;
        public Vector2 OutlineSize;
        public ContentSizeFitter.FitMode FitContent;
        public bool RootContainer;
        /// <summary>
        /// Screen position for root containers. If Vector2.zero, uses full-screen layout.
        /// If non-zero, positions the panel at the specified screen coordinates.
        /// Only used when RootContainer is true.
        /// </summary>
        public Vector2 ScreenPosition;

        public static PanelOptions Default(PanelLayoutType layoutType = PanelLayoutType.Vertical)
        {
            return new PanelOptions
            {
                Name = "Panel",
                LayoutGroupOptions = layoutType == PanelLayoutType.Vertical ? 
                    LayoutGroupOptions.Vertical() : LayoutGroupOptions.Horizontal(),
                LayoutElementOptions = LayoutElementOptions.Flexible(),
                ImageOptions = new ImageOptions { Color = new Color(0.15f, 0.15f, 0.15f, 1f) },
                HasBackground = true,
                HasOutline = false,
                OutlineColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                OutlineSize = Vector2.one,
                FitContent = ContentSizeFitter.FitMode.Unconstrained,
                RootContainer = false,
                ScreenPosition = Vector2.zero
            };
        }

        public static PanelOptions RootContainerPanel(PanelLayoutType layoutType = PanelLayoutType.Vertical)
        {
            var options = Default(layoutType);
            options.RootContainer = true;
            options.LayoutElementOptions = LayoutElementOptions.Flexible(1, 1);
            return options;
        }

        public static PanelOptions FixedSizePanel(Vector2 size, PanelLayoutType layoutType = PanelLayoutType.Vertical)
        {
            var options = Default(layoutType);
            options.LayoutElementOptions = LayoutElementOptions.Fixed(size.x, size.y);
            return options;
        }

        public static PanelOptions FlexiblePanel(int flexWidth = 1, int flexHeight = 1, PanelLayoutType layoutType = PanelLayoutType.Vertical)
        {
            var options = Default(layoutType);
            options.LayoutElementOptions = LayoutElementOptions.Flexible(flexWidth, flexHeight);
            return options;
        }

        public static PanelOptions PositionedRootPanel(Vector2 screenPosition, Vector2 size, PanelLayoutType layoutType = PanelLayoutType.Vertical)
        {
            var options = Default(layoutType);
            options.RootContainer = true;
            options.ScreenPosition = screenPosition;
            options.LayoutElementOptions = LayoutElementOptions.Fixed(size.x, size.y);
            return options;
        }
    }

    public static class PanelFactory
    {      
        public static GameObject CreatePanel(Transform parent, PanelOptions options = default)
        {
            // Use default options if none provided
            if (options.Name == null)
                options = PanelOptions.Default();
            
            var panelObj = new GameObject(options.Name ?? "Panel");
            panelObj.transform.SetParent(parent, false);
            
            // Set up RectTransform if this is a root container
            if (options.RootContainer)
            {
                SetupRootContainer(panelObj, options.ScreenPosition, options.LayoutElementOptions);
            }
            
            // Add layout element for size control
            LayoutFactory.CreateLayoutElement(panelObj.transform, options.LayoutElementOptions);
            
            // Add background image if needed
            if (options.HasBackground)
            {
                ImageFactory.CreateImage(panelObj.transform, options.ImageOptions);
                
                if (options.HasOutline)
                {
                    var outline = panelObj.AddComponent<Outline>();
                    outline.effectColor = options.OutlineColor;
                    outline.effectDistance = options.OutlineSize;
                }
            }
            
            // Add appropriate layout group
            if (options.LayoutGroupOptions.layoutType != PanelLayoutType.None)
            {
                LayoutFactory.CreateLayoutGroup(panelObj.transform, options.LayoutGroupOptions);
            }
            
            // Add content size fitter if needed
            if (options.FitContent != ContentSizeFitter.FitMode.Unconstrained)
            {
                var sizeFitter = panelObj.AddComponent<ContentSizeFitter>();
                sizeFitter.horizontalFit = options.FitContent;
                sizeFitter.verticalFit = options.FitContent;
            }
            
            return panelObj;
        }

        public static GameObject CreateVerticalPanel(Transform parent, string name = "VerticalPanel", bool rootContainer = false, Vector2 screenPosition = default)
        {
            var options = rootContainer ? 
                PanelOptions.RootContainerPanel(PanelLayoutType.Vertical) :
                PanelOptions.Default(PanelLayoutType.Vertical);
            options.Name = name;
            options.ScreenPosition = screenPosition;
            return CreatePanel(parent, options);
        }

        public static GameObject CreateHorizontalPanel(Transform parent, string name = "HorizontalPanel", bool rootContainer = false, Vector2 screenPosition = default)
        {
            var options = rootContainer ? 
                PanelOptions.RootContainerPanel(PanelLayoutType.Horizontal) :
                PanelOptions.Default(PanelLayoutType.Horizontal);
            options.Name = name;
            options.ScreenPosition = screenPosition;
            return CreatePanel(parent, options);
        }

        public static GameObject CreateGridPanel(Transform parent, Vector2 cellSize, string name = "GridPanel", bool rootContainer = false, Vector2 screenPosition = default)
        {
            var options = rootContainer ? 
                PanelOptions.RootContainerPanel(PanelLayoutType.Grid) :
                PanelOptions.Default(PanelLayoutType.Grid);
            options.Name = name;
            options.LayoutGroupOptions = LayoutGroupOptions.Grid(cellSize);
            options.ScreenPosition = screenPosition;
            return CreatePanel(parent, options);
        }

        public static GameObject CreateNoLayoutPanel(Transform parent, string name = "NoLayoutPanel", bool rootContainer = false, Vector2 screenPosition = default)
        {
            var options = rootContainer ? 
                PanelOptions.RootContainerPanel(PanelLayoutType.None) :
                PanelOptions.Default(PanelLayoutType.None);
            options.Name = name;
            options.LayoutGroupOptions = new LayoutGroupOptions { layoutType = PanelLayoutType.None };
            options.ScreenPosition = screenPosition;
            return CreatePanel(parent, options);
        }

        private static void SetupRootContainer(GameObject panelObj, Vector2 screenPosition, LayoutElementOptions layoutOptions)
        {
            var rectTransform = panelObj.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = panelObj.AddComponent<RectTransform>();
            }

            if (screenPosition == Vector2.zero)
            {
                // Set up as full-screen container (original behavior)
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }
            else
            {
                // Set up as positioned container using screen position
                // Use center anchoring for positioned panels
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = screenPosition;
                
                // Explicitly set size from layout options
                float width = layoutOptions.preferredWidth > 0 ? layoutOptions.preferredWidth : 
                             layoutOptions.minWidth > 0 ? layoutOptions.minWidth : 400f; // fallback default
                float height = layoutOptions.preferredHeight > 0 ? layoutOptions.preferredHeight : 
                              layoutOptions.minHeight > 0 ? layoutOptions.minHeight : 300f; // fallback default
                
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }
    }
}