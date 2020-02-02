using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GridLayoutGroupCycle : GridLayoutGroup, ILayoutGroupCycle
{
    [SerializeField]
    protected ScrollRect m_ScrollRect;
    public ScrollRect scrollRect { get { return m_ScrollRect; } set { m_ScrollRect = value; } }

    [SerializeField]
    protected uint m_Capacity;
    public uint capacity { get { return m_Capacity; } set { SetProperty(ref m_Capacity, value); } }
    public OnPopulateChild onPopulateChild { get; set; }

    protected Vector2 m_NormalizedPosition = Vector2.zero;
    private int m_StartIndex = -1;
    private int[] m_ChildIndexMap = null;
    private List<GameObject> m_PendingDeactiveList = null;
    private List<GameObject> m_PendingPopulateList = null;

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

        SetCellsAlongAxisCycle(1, true);
    }

    // this function makes sure Populate() is called while property capacity doesn't when capacity stay unchanged
    public void SetCapacity(uint value)
    {
        capacity = value;

        Populate();
    }

    [ContextMenu("Populate")]
    public void Populate()
    {
        m_StartIndex = -1;
        m_ChildIndexMap = Enumerable.Repeat(-1, rectChildren.Count).ToArray();

        SetDirty();
    }

    [ContextMenu("Reset Position")]
    public virtual void ResetPosition()
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

    public override void CalculateLayoutInputHorizontal()
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

        if (m_ChildIndexMap == null || m_ChildIndexMap.Length != rectChildren.Count)
        {
            m_ChildIndexMap = Enumerable.Repeat(-1, rectChildren.Count).ToArray();
        }

        m_Tracker.Clear();

        int minColumns = 0;
        int preferredColumns = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minColumns = preferredColumns = m_ConstraintCount;
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minColumns = preferredColumns = Mathf.CeilToInt(capacity / (float)m_ConstraintCount - 0.001f);
        }
        else
        {
            minColumns = 1;
            preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(capacity));
        }

        SetLayoutInputForAxis(
            padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x,
            padding.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x,
            -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        int minRows = 0;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            minRows = Mathf.CeilToInt(capacity / (float)m_ConstraintCount - 0.001f);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            minRows = m_ConstraintCount;
        }
        else
        {
            float width = rectTransform.rect.width;
            int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
            minRows = Mathf.CeilToInt(capacity / (float)cellCountX);
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
        SetCellsAlongAxisCycle(0);
    }

    /// <summary>
    /// Called by the layout system
    /// Also see ILayoutElement
    /// </summary>
    public override void SetLayoutVertical()
    {
        SetCellsAlongAxisCycle(1);
    }

    private void SetCellsAlongAxisCycle(int axis, bool cycling = false)
    {
        // Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
        // and only vertical values when invoked for the vertical axis.
        // However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
        // Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
        // and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.

        if (axis == 0)
        {
            // Only set the sizes when invoked for horizontal axis, not the positions.
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform rect = rectChildren[i];

                m_Tracker.Add(this, rect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.SizeDelta);

                rect.anchorMin = Vector2.up;
                rect.anchorMax = Vector2.up;
                rect.sizeDelta = cellSize;
            }
            return;
        }

        float width = rectTransform.rect.size.x;
        float height = rectTransform.rect.size.y;

        int cellCountX = 1;
        int cellCountY = 1;
        if (m_Constraint == Constraint.FixedColumnCount)
        {
            cellCountX = m_ConstraintCount;

            if (capacity > cellCountX)
                cellCountY = (int)capacity / cellCountX + (capacity % cellCountX > 0 ? 1 : 0);
        }
        else if (m_Constraint == Constraint.FixedRowCount)
        {
            cellCountY = m_ConstraintCount;

            if (capacity > cellCountY)
                cellCountX = (int)capacity / cellCountY + (capacity % cellCountY > 0 ? 1 : 0);
        }
        else
        {
            if (cellSize.x + spacing.x <= 0)
                cellCountX = int.MaxValue;
            else
                cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

            if (cellSize.y + spacing.y <= 0)
                cellCountY = int.MaxValue;
            else
                cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
        }

        int cornerX = (int)startCorner % 2;
        int cornerY = (int)startCorner / 2;

        int cellsPerMainAxis, actualCellCountX, actualCellCountY;
        if (startAxis == Axis.Horizontal)
        {
            cellsPerMainAxis = cellCountX;
            actualCellCountX = cellCountX; //Mathf.Clamp(cellCountX, 1, (int)capacity);
            actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(capacity / (float)cellsPerMainAxis));
        }
        else
        {
            cellsPerMainAxis = cellCountY;
            actualCellCountY = cellCountY; //Mathf.Clamp(cellCountY, 1, (int)capacity);
            actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(capacity / (float)cellsPerMainAxis));
        }

        Vector2 requiredSpace = new Vector2(actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x, actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y);
        Vector2 startOffset = new Vector2(GetStartOffset(0, requiredSpace.x), GetStartOffset(1, requiredSpace.y));

        var newStartIndex = 0;
        if (startAxis == Axis.Vertical)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            var currentOffset = Mathf.Max(0, width - scrollRect.viewport.rect.size.x) * m_NormalizedPosition.x;
            var startCol = Mathf.FloorToInt(Mathf.Max(0, currentOffset - startOffset.x) / (cellSize.x + spacing.x));
            newStartIndex = startCol * actualCellCountY;
        }
        else
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var currentOffset = Mathf.Max(0, height - scrollRect.viewport.rect.size.y) * m_NormalizedPosition.y;
            var startRow = Mathf.FloorToInt(Mathf.Max(0, currentOffset - startOffset.y) / (cellSize.y + spacing.y));
            newStartIndex = startRow * actualCellCountX;
        }


        if (!cycling || newStartIndex != m_StartIndex)
        {
            m_StartIndex = newStartIndex;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                int index = i + m_StartIndex;
                if (index < capacity || index < rectChildren.Count)
                {
                    int childIndex = index % rectChildren.Count;
                    if (!cycling || m_ChildIndexMap[childIndex] != index)
                    {
                        var child = rectChildren[childIndex];

                        int positionX;
                        int positionY;
                        if (startAxis == Axis.Horizontal)
                        {
                            positionX = index % cellsPerMainAxis;
                            positionY = index / cellsPerMainAxis;
                        }
                        else
                        {
                            positionX = index / cellsPerMainAxis;
                            positionY = index % cellsPerMainAxis;
                        }
                        int col = positionX;
                        int row = positionY;

                        if (cornerX == 1)
                            positionX = actualCellCountX - 1 - positionX;
                        if (cornerY == 1)
                            positionY = actualCellCountY - 1 - positionY;

                        SetChildAlongAxis(child, 0, startOffset.x + (cellSize[0] + spacing[0]) * positionX, cellSize[0]);
                        SetChildAlongAxis(child, 1, startOffset.y + (cellSize[1] + spacing[1]) * positionY, cellSize[1]);

                        if (child.gameObject.activeSelf != index < capacity)
                        {
                            // the code commented below will trigger error while rebuilding layouts: Trying to remove xxx from rebuild list while we are already inside a rebuild loop
                            // child.gameObject.SetActive(index < capacity);
                            if (index < capacity)
                            {
                                child.gameObject.SetActive(true);
                            }
                            else
                            {
                                // pending deactivation of children to late update to avoid error metioned above
                                if (m_PendingDeactiveList == null)
                                {
                                    m_PendingDeactiveList = ListPool<GameObject>.Get();
                                }

                                m_PendingDeactiveList.Add(child.gameObject);
                            }
                        }

                        if (index < capacity && m_ChildIndexMap[childIndex] != index)
                        {
                            m_ChildIndexMap[childIndex] = index;

                            if (m_PendingPopulateList == null)
                            {
                                m_PendingPopulateList = ListPool<GameObject>.Get();
                            }

                            m_PendingPopulateList.Add(child.gameObject);

                        }
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        if (m_PendingDeactiveList != null)
        {
            foreach (var go in m_PendingDeactiveList)
            {
                go.SetActive(false);
            }

            ListPool<GameObject>.Release(m_PendingDeactiveList);

            m_PendingDeactiveList = null;
        }
        if (m_PendingPopulateList != null)
        {
            foreach (var go in m_PendingPopulateList)
            {
                var index = m_ChildIndexMap[go.transform.GetSiblingIndex()];

                onPopulateChild?.Invoke(go, index);

                Debug.Log($"GridLayoutGroupCycle.PopulateChild({index})");
            }

            ListPool<GameObject>.Release(m_PendingPopulateList);

            m_PendingPopulateList = null;
        }
    }
}
