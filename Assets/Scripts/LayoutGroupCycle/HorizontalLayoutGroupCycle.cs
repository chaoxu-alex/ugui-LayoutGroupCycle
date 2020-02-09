using UnityEngine;

public class HorizontalLayoutGroupCycle : HorizontalOrVerticalLayoutGroupCycle
{
    protected HorizontalLayoutGroupCycle() { }

    protected override void Start()
    {
        if (scrollRect)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
        }
    }

    protected override void OnScrolling(Vector2 normalizedPosition)
    {
        m_NormalizedPosition = normalizedPosition;

        // there are chances that capacity and m_CellInfoMap.Length doesn't match when changing capacity frequently in editor.
        if (capacity == m_CellInfoMap.Length)
        {
            SetChildrenAlongAxisCycle(0, false, true);

            SetChildrenAlongAxisCycle(1, false, true);
        }
    }

    [ContextMenu("Reset Position")]
    public override void ResetPosition()
    {
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.horizontalNormalizedPosition = reversed ? 1.0f : 0.0f;
        }
    }

    public override void Locate(uint index)
    {
        if (m_CellInfoMap != null && 0 <= index && index < m_CellInfoMap.Length)
        {
            var contentSize = rectTransform.rect.size[0];
            var viewSize = scrollRect.viewport.rect.size[0];
            var scrollSize = Mathf.Max(0, contentSize - viewSize);
            var viewMin = m_NormalizedPosition[0] * scrollSize;
            var viewMax = viewMin + viewSize;
            var cellInfo = m_CellInfoMap[index];

            if (cellInfo.pos[0] < viewMin)
            {
                scrollRect.horizontalNormalizedPosition = cellInfo.pos[0] / scrollSize;
            }
            else if (cellInfo.pos[0] + cellInfo.size[0] > viewMax)
            {
                scrollRect.horizontalNormalizedPosition = (cellInfo.pos[0] + cellInfo.size[0] - viewSize) / scrollSize;
            }
        }
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        CalcAlongAxisCycle(0, false);
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void CalculateLayoutInputVertical()
    {
        CalcAlongAxisCycle(1, false);
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void SetLayoutHorizontal()
    {
        CalcCellAlongAxisCycle(0, false);
        SetChildrenAlongAxisCycle(0, false);
    }

    /// <summary>
    /// Called by the layout system. Also see ILayoutElement
    /// </summary>
    public override void SetLayoutVertical()
    {
        CalcCellAlongAxisCycle(1, false);
        SetChildrenAlongAxisCycle(1, false);
    }
}
