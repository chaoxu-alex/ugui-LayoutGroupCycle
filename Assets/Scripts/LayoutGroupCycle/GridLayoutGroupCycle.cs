using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ContentSizeFitter))]
public class GridLayoutGroupCycle : GridLayoutGroup, ILayoutGroupCycle
{
    [SerializeField]
    protected ScrollRect m_ScrollRect;
    public ScrollRect scrollRect { get { return m_ScrollRect; } set { m_ScrollRect = value; } }

    [SerializeField]
    protected uint m_Size;
    public uint size { get { return m_Size; } set { SetProperty(ref m_Size, value); } }
    public OnPopulateChild onPopulateChild { get; set; }

    protected int m_actualCellCountX = 0;
    protected int m_actualCellCountY = 0;
    protected int m_StartIndex = -1;
    protected int[] m_ChildIndexMap = null;
    protected Vector2 m_startOffset = Vector2.zero;
    protected Vector2 m_NormalizedPosition = Vector2.zero;
    protected Vector2[] m_CellPosMap = null;
    protected List<GameObject> m_PendingActiveList = new List<GameObject>();
    protected List<GameObject> m_PendingDeactiveList = new List<GameObject>();
    protected List<GameObject> m_PendingPopulateList = new List<GameObject>();

    protected override void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrolling);

            if (scrollRect.horizontal == scrollRect.vertical)
            {
                Debug.LogError("ScrollRect should have only one scroll dimension enabled while used with GridLayoutGroupCycle.");
                scrollRect.vertical = true;
                scrollRect.horizontal = false;
            }
        }
        else
        {
            Debug.LogError("GridLayoutGroupCycle should be used with ScrollRect.");
        }

        if (Application.isPlaying)
        {
            foreach (Transform trans in rectTransform)
            {
                trans.gameObject.SetActive(false);
            }
        }
    }

    protected virtual void OnScrolling(Vector2 normalizedPosistion)
    {
        if (scrollRect != null)
        {
            if (scrollRect.horizontal)
            {
                int cornerX = (int)startCorner % 2;
                m_NormalizedPosition = cornerX == 0 ? normalizedPosistion : Vector2.one - normalizedPosistion;
            }
            else if (scrollRect.vertical)
            {
                int cornerY = (int)startCorner / 2;
                m_NormalizedPosition = cornerY == 0 ? Vector2.one - normalizedPosistion : normalizedPosistion;
            }
        }

        UpdateChildrenPositions(true);
    }

    // this function makes sure Populate() is called while property size doesn't when size stay unchanged
    public void SetSize(uint value)
    {
        size = value;

        Populate();
    }

    [ContextMenu("Populate")]
    public void Populate()
    {
        m_StartIndex = -1;

        if (m_ChildIndexMap != null)
        {
            for (var i = 0; i < m_ChildIndexMap.Length; ++i)
            {
                m_ChildIndexMap[i] = -1;
            }
        }

        //SetDirty();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        if (scrollRect != null)
        {
            int cornerX = (int)startCorner % 2;
            int cornerY = (int)startCorner / 2;

            scrollRect.StopMovement();

            if (scrollRect.horizontal)
            {
                scrollRect.horizontalNormalizedPosition = cornerX == 0 ? 0.0f : 1.0f;
            }
            else if (scrollRect.vertical)
            {
                scrollRect.verticalNormalizedPosition = cornerY == 1 ? 0.0f : 1.0f;
            }
        }
    }

    public void Locate(uint index)
    {
        if (scrollRect.horizontal)
        {
            LocateAlongAxis(0, index);
        }
        else if (scrollRect.vertical)
        {
            LocateAlongAxis(1, index);
        }
    }

    protected void LocateAlongAxis(int axis, uint index)
    {
        if (m_CellPosMap != null && index < m_CellPosMap.Length)
        {
            var contentSize = rectTransform.rect.size[axis];
            var viewSize = scrollRect.viewport.rect.size[axis];
            var scrollSize = Mathf.Max(0, contentSize - viewSize);
            var viewMin = (axis == 0 ? scrollRect.normalizedPosition[axis] : 1 - scrollRect.normalizedPosition[axis]) * scrollSize;
            var viewMax = viewMin + viewSize;
            var cellPos = m_CellPosMap[index];

            var normalizedPosition = scrollRect.normalizedPosition;
            if (cellPos[axis] < viewMin)
            {
                normalizedPosition[axis] = cellPos[axis] / scrollSize;
                if (axis == 1) normalizedPosition[axis] = 1 - normalizedPosition[axis];
            }
            else if (cellPos[axis] + cellSize[axis] > viewMax)
            {
                normalizedPosition[axis] = (cellPos[axis] + cellSize[axis] - viewSize) / scrollSize;
                if (axis == 1) normalizedPosition[axis] = 1 - normalizedPosition[axis];
            }

            scrollRect.normalizedPosition = normalizedPosition;
        }
    }

    public override void CalculateLayoutInputHorizontal()
    {
        var contentSizeFitter = GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            if (startAxis == Axis.Horizontal)
            {
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        UpdateChildren();

        int minColumns = 0;
        int preferredColumns = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minColumns = preferredColumns = m_ConstraintCount;
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minColumns = preferredColumns = Mathf.CeilToInt(size / (float)m_ConstraintCount - 0.001f);
        }
        else
        {
            minColumns = 1;
            preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(size));
        }

        SetLayoutInputForAxis(
            padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x,
            padding.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x,
            -1, 0);
    }

    protected void UpdateChildren()
    {
        rectChildren.Clear();
        var toIgnoreList = ListPool<Component>.Get();
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var rect = rectTransform.GetChild(i) as RectTransform;
            if (rect == null)
                continue;

            rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

            if (toIgnoreList.Count == 0)
            {
                rectChildren.Add(rect);
                continue;
            }

            for (int j = 0; j < toIgnoreList.Count; j++)
            {
                var ignorer = (ILayoutIgnorer)toIgnoreList[j];
                if (!ignorer.ignoreLayout)
                {
                    rectChildren.Add(rect);
                    break;
                }
            }
        }

        ListPool<Component>.Release(toIgnoreList);

        m_Tracker.Clear();

        if (m_ChildIndexMap == null || m_ChildIndexMap.Length != rectChildren.Count)
        {
            m_ChildIndexMap = Enumerable.Repeat(-1, rectChildren.Count).ToArray();
        }
    }

    public override void CalculateLayoutInputVertical()
    {
        int minRows = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minRows = Mathf.CeilToInt(size / (float)m_ConstraintCount - 0.001f);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minRows = m_ConstraintCount;
        }
        else
        {
            float width = rectTransform.rect.width;
            int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            minRows = Mathf.CeilToInt(size / (float)cellCountX);
        }

        float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;
        SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
    }

    /// <summary>
    /// Called by the layout system
    /// Also see ILayoutElement
    /// </summary>
    public override void SetLayoutHorizontal()
    {
        UpdateChildrenLayout();
    }

    /// <summary>
    /// Called by the layout system
    /// Also see ILayoutElement
    /// </summary>
    public override void SetLayoutVertical()
    {
        CalculateCellPositions();

        UpdateChildrenPositions();
    }

    protected void UpdateChildrenLayout()
    {
        // Only set the sizes when invoked for horizontal axis, not the positions.
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform rect = rectChildren[i];

            m_Tracker.Add(this, rect, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);

            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.sizeDelta = cellSize;
        }
    }

    protected void CalculateCellPositions()
    {
        float width = rectTransform.rect.size.x;
        float height = rectTransform.rect.size.y;

        int cellCountX = 1;
        int cellCountY = 1;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            cellCountX = m_ConstraintCount;

            if (size > cellCountX)
            {
                cellCountY = ((int)size + cellCountX - 1) / cellCountX;
            }
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            cellCountY = m_ConstraintCount;

            if (size > cellCountY)
            {
                cellCountX = ((int)size + cellCountY - 1) / cellCountY;
            }
        }
        else
        {
            if (cellSize.x + spacing.x <= 0)
            {
                cellCountX = int.MaxValue;
            }
            else
            {
                cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            }

            if (cellSize.y + spacing.y <= 0)
            {
                cellCountY = int.MaxValue;
            }
            else
            {
                cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
            }
        }

        int cornerX = (int)startCorner % 2;
        int cornerY = (int)startCorner / 2;

        if (startAxis == Axis.Horizontal)
        {
            m_actualCellCountX = cellCountX;
            m_actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(size / (float)cellCountX));
        }
        else
        {
            m_actualCellCountY = cellCountY;
            m_actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(size / (float)cellCountY));
        }

        Vector2 requiredSpace = new Vector2(m_actualCellCountX * cellSize.x + (m_actualCellCountX - 1) * spacing.x, m_actualCellCountY * cellSize.y + (m_actualCellCountY - 1) * spacing.y);
        m_startOffset = new Vector2(GetStartOffset(0, requiredSpace.x), GetStartOffset(1, requiredSpace.y));

        if (m_CellPosMap == null || m_CellPosMap.Length != size)
        {
            m_CellPosMap = new Vector2[size];
        }

        for (int i = 0; i < size; i++)
        {
            int positionX;
            int positionY;
            if (startAxis == Axis.Horizontal)
            {
                positionX = i % m_actualCellCountX;
                positionY = i / m_actualCellCountX;
            }
            else
            {
                positionX = i / m_actualCellCountY;
                positionY = i % m_actualCellCountY;
            }

            if (cornerX == 1)
            {
                positionX = m_actualCellCountX - 1 - positionX;
            }
            if (cornerY == 1)
            {
                positionY = m_actualCellCountY - 1 - positionY;
            }

            m_CellPosMap[i].x = m_startOffset.x + (cellSize[0] + spacing[0]) * positionX;
            m_CellPosMap[i].y = m_startOffset.y + (cellSize[1] + spacing[1]) * positionY;
        }
    }

    protected void UpdateChildrenPositions(bool cycling = false)
    {
        float width = rectTransform.rect.size.x;
        float height = rectTransform.rect.size.y;

        var newStartIndex = 0;
        if (startAxis == Axis.Horizontal)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var currentOffset = Mathf.Max(0, height - scrollRect.viewport.rect.size.y) * m_NormalizedPosition.y;
            var startRow = Mathf.FloorToInt(Mathf.Max(0, currentOffset - m_startOffset.y) / (cellSize.y + spacing.y));
            newStartIndex = startRow * m_actualCellCountX;
        }
        else
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            var currentOffset = Mathf.Max(0, width - scrollRect.viewport.rect.size.x) * m_NormalizedPosition.x;
            var startCol = Mathf.FloorToInt(Mathf.Max(0, currentOffset - m_startOffset.x) / (cellSize.x + spacing.x));
            newStartIndex = startCol * m_actualCellCountY;
        }

        if (!cycling || newStartIndex != m_StartIndex)
        {
            m_StartIndex = newStartIndex;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                int index = i + m_StartIndex;
                int childIndex = index % rectChildren.Count;
                var child = rectChildren[childIndex];

                if (index < size)
                {
                    if (!cycling || m_ChildIndexMap[childIndex] != index)
                    {
                        SetChildAlongAxis(child, 0, m_CellPosMap[index].x, cellSize[0]);
                        SetChildAlongAxis(child, 1, m_CellPosMap[index].y, cellSize[1]);

                        if (m_ChildIndexMap[childIndex] != index)
                        {
                            m_ChildIndexMap[childIndex] = index;
                            m_PendingPopulateList.Add(child.gameObject);
                        }
                    }

                    if (!child.gameObject.activeSelf)
                    {
                        // pending activation of children to late update as onPopulateChild to make sure they are in the same frame
                        m_PendingActiveList.Add(child.gameObject);
                    }
                }
                else
                {
                    if (child.gameObject.activeSelf)
                    {
                        // pending deactivation of children to late update to removing graphic from rebuild list while we are already inside a rebuild loop
                        m_PendingDeactiveList.Add(child.gameObject);
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        if (m_PendingActiveList.Count > 0)
        {
            m_PendingActiveList.ForEach(go => go.SetActive(true));

            m_PendingActiveList.Clear();
        }

        if (m_PendingDeactiveList.Count > 0)
        {
            m_PendingDeactiveList.ForEach(go => go.SetActive(false));

            m_PendingDeactiveList.Clear();
        }

        if (m_PendingPopulateList.Count > 0)
        {
            m_PendingPopulateList.ForEach(go =>
            {
                var index = m_ChildIndexMap[go.transform.GetSiblingIndex()];

                if (index >= 0)
                {
                    onPopulateChild?.Invoke(go, index);
                }

                Debug.Log($"GridLayoutGroupCycle.PopulateChild({index})");
            });

            m_PendingPopulateList.Clear();
        }
    }
}
