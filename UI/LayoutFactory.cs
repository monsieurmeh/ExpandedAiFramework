using UnityEngine;
using UnityEngine.UI;


namespace ExpandedAiFramework.UI
{
    [System.Serializable]
    public struct LayoutElementOptions
    {
        public float minWidth;
        public float minHeight;
        public float preferredWidth;
        public float preferredHeight;
        public int flexibleWidth;
        public int flexibleHeight;
        public bool ignoreLayout;

        public static LayoutElementOptions Flexible(int flexWidth = 1, int flexHeight = 1)
        {
            return new LayoutElementOptions
            {
                minWidth = -1,
                minHeight = -1,
                preferredWidth = -1,
                preferredHeight = -1,
                flexibleWidth = flexWidth,
                flexibleHeight = flexHeight,
                ignoreLayout = false
            };
        }

        public static LayoutElementOptions Fixed(float width, float height)
        {
            return new LayoutElementOptions
            {
                minWidth = width,
                minHeight = height,
                preferredWidth = width,
                preferredHeight = height,
                flexibleWidth = 0,
                flexibleHeight = 0,
                ignoreLayout = false
            };
        }

        public static LayoutElementOptions MinSize(float minWidth, float minHeight, int flexWidth = 1, int flexHeight = 1)
        {
            return new LayoutElementOptions
            {
                minWidth = minWidth,
                minHeight = minHeight,
                preferredWidth = -1,
                preferredHeight = -1,
                flexibleWidth = flexWidth,
                flexibleHeight = flexHeight,
                ignoreLayout = false
            };
        }

        public static LayoutElementOptions PreferredSize(float preferredWidth, float preferredHeight, int flexWidth = 0, int flexHeight = 0)
        {
            return new LayoutElementOptions
            {
                minWidth = -1,
                minHeight = -1,
                preferredWidth = preferredWidth,
                preferredHeight = preferredHeight,
                flexibleWidth = flexWidth,
                flexibleHeight = flexHeight,
                ignoreLayout = false
            };
        }
    }

    [System.Serializable]
    public struct LayoutGroupOptions
    {
        public PanelLayoutType layoutType;
        public RectOffset padding;
        public float spacing;
        public bool childControlWidth;
        public bool childControlHeight;
        public bool childForceExpandWidth;
        public bool childForceExpandHeight;
        public TextAnchor childAlignment;
        
        // Grid-specific options
        public Vector2 gridCellSize;
        public GridLayoutGroup.Constraint gridConstraint;
        public int gridConstraintCount;

        public static LayoutGroupOptions Vertical(float spacing = 5f, RectOffset padding = null)
        {
            return new LayoutGroupOptions
            {
                layoutType = PanelLayoutType.Vertical,
                padding = padding ?? new RectOffset(10, 10, 10, 10),
                spacing = spacing,
                childControlWidth = true,
                childControlHeight = true,
                childForceExpandWidth = false,
                childForceExpandHeight = false,
                childAlignment = TextAnchor.UpperCenter,
                gridCellSize = new Vector2(100, 100),
                gridConstraint = GridLayoutGroup.Constraint.Flexible,
                gridConstraintCount = 2
            };
        }

        public static LayoutGroupOptions Horizontal(float spacing = 5f, RectOffset padding = null)
        {
            return new LayoutGroupOptions
            {
                layoutType = PanelLayoutType.Horizontal,
                padding = padding ?? new RectOffset(10, 10, 10, 10),
                spacing = spacing,
                childControlWidth = true,
                childControlHeight = true,
                childForceExpandWidth = false,
                childForceExpandHeight = false,
                childAlignment = TextAnchor.MiddleLeft,
                gridCellSize = new Vector2(100, 100),
                gridConstraint = GridLayoutGroup.Constraint.Flexible,
                gridConstraintCount = 2
            };
        }

        public static LayoutGroupOptions Grid(Vector2 cellSize, float spacing = 5f, RectOffset padding = null, 
            GridLayoutGroup.Constraint constraint = GridLayoutGroup.Constraint.Flexible, int constraintCount = 2)
        {
            return new LayoutGroupOptions
            {
                layoutType = PanelLayoutType.Grid,
                padding = padding ?? new RectOffset(10, 10, 10, 10),
                spacing = spacing,
                childControlWidth = false,
                childControlHeight = false,
                childForceExpandWidth = false,
                childForceExpandHeight = false,
                childAlignment = TextAnchor.UpperLeft,
                gridCellSize = cellSize,
                gridConstraint = constraint,
                gridConstraintCount = constraintCount
            };
        }
    }

    public static class LayoutFactory
    {            
        public static LayoutGroup CreateLayoutGroup(Transform parent, LayoutGroupOptions options)
        {
            LayoutGroup layoutGroup;
            switch (options.layoutType)
            {
                case PanelLayoutType.Vertical:
                    var verticalLayout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
                    SetHorizontalOrVerticalLayoutGroup(verticalLayout, options);
                    layoutGroup = verticalLayout;
                    break;
                case PanelLayoutType.Horizontal:
                    var horizontalLayout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
                    SetHorizontalOrVerticalLayoutGroup(horizontalLayout, options);
                    layoutGroup = horizontalLayout;
                    break;
                case PanelLayoutType.Grid:
                    var gridLayout = parent.gameObject.AddComponent<GridLayoutGroup>();
                    SetGridLayoutGroup(gridLayout, options);
                    layoutGroup = gridLayout;
                    break;
                default:
                    throw new System.Exception($"Invalid layout type: {options.layoutType}");
            }
            return layoutGroup;
        }

        public static HorizontalOrVerticalLayoutGroup CreateHorizontalOrVerticalLayoutGroup(Transform parent, LayoutGroupOptions options)
        {
            HorizontalOrVerticalLayoutGroup layoutGroup;
            switch (options.layoutType)
            {
                case PanelLayoutType.Vertical:
                    layoutGroup = parent.gameObject.AddComponent<VerticalLayoutGroup>();
                    break;
                case PanelLayoutType.Horizontal:
                    layoutGroup = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
                    break;
                default:
                    throw new System.Exception($"Invalid layout type for HorizontalOrVertical: {options.layoutType}");
            }
            SetHorizontalOrVerticalLayoutGroup(layoutGroup, options);
            return layoutGroup;
        }


        private static void SetHorizontalOrVerticalLayoutGroup(HorizontalOrVerticalLayoutGroup layoutGroup, LayoutGroupOptions options)
        {
            layoutGroup.m_Padding = options.padding;
            layoutGroup.m_Spacing = options.spacing;
            layoutGroup.m_ChildControlWidth = options.childControlWidth;
            layoutGroup.m_ChildControlHeight = options.childControlHeight;
            layoutGroup.m_ChildForceExpandWidth = options.childForceExpandWidth;
            layoutGroup.m_ChildForceExpandHeight = options.childForceExpandHeight;
            layoutGroup.m_ChildAlignment = options.childAlignment;
        }

        private static void SetGridLayoutGroup(GridLayoutGroup layoutGroup, LayoutGroupOptions options)
        {
            layoutGroup.padding = options.padding;
            layoutGroup.spacing = new Vector2(options.spacing, options.spacing);
            layoutGroup.childAlignment = options.childAlignment;
            layoutGroup.cellSize = options.gridCellSize;
            layoutGroup.constraint = options.gridConstraint;
            layoutGroup.constraintCount = options.gridConstraintCount;
        }


        public static LayoutElement CreateLayoutElement(Transform parent, LayoutElementOptions options)
        {
            var layoutElement = parent.gameObject.AddComponent<LayoutElement>();
            SetLayoutElement(layoutElement, options);
            return layoutElement;
        }


        private static void SetLayoutElement(LayoutElement layoutElement, LayoutElementOptions options)
        {
            layoutElement.minWidth = options.minWidth;
            layoutElement.minHeight = options.minHeight;
            layoutElement.preferredWidth = options.preferredWidth;
            layoutElement.preferredHeight = options.preferredHeight;
            layoutElement.flexibleWidth = options.flexibleWidth;
            layoutElement.flexibleHeight = options.flexibleHeight;
            layoutElement.ignoreLayout = options.ignoreLayout;
        }
    }
}