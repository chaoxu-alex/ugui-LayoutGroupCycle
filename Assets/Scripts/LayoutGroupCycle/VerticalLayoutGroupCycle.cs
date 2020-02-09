using UnityEngine;

public class VerticalLayoutGroupCycle : HorizontalOrVerticalLayoutGroupCycle
{
    protected VerticalLayoutGroupCycle() { }

    protected override void Start()
    {
        if (scrollRect)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }
    }

    protected override void OnScrolling(Vector2 normalizedPosition)
    {
        // vertical layout group cycle is aligned from top to bottom by default while y axis growth from bottom to top.
        m_NormalizedPosition = Vector2.one - normalizedPosition;

        // there are chances that capacity and m_CellInfoMap.Length doesn't match when changing capacity frequently in editor.
        if (capacity == m_CellInfoMap.Length)
        {
            SetChildrenAlongAxisCycle(0, true, true);

            SetChildrenAlongAxisCycle(1, true, true);
        }
    }

    [ContextMenu("Reset Position")]
    public override void ResetPosition()
    {
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = reversed ? 0.0f : 1.0f;
        }
    }

    public override void Locate(uint index)
    {
        if (m_CellInfoMap != null && 0 <= index && index < m_CellInfoMap.Length)
        {
            var contentSize = rectTransform.rect.size[1];
            var viewSize = scrollRect.viewport.rect.size[1];
            var scrollSize = Mathf.Max(0, contentSize - viewSize);
            var viewMin = m_NormalizedPosition[1] * scrollSize;
            var viewMax = viewMin + viewSize;
            var cellInfo = m_CellInfoMap[index];

            if (cellInfo.pos[1] < viewMin)
            {
                scrollRect.verticalNormalizedPosition = 1 - cellInfo.pos[1] / scrollSize;
            }
            else if (cellInfo.pos[1] + cellInfo.size[1] > viewMax)
            {
                scrollRect.verticalNormalizedPosition = 1 - (cellInfo.pos[1] + cellInfo.size[1] - viewSize) / scrollSize;;
            }
        }
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        CalcAlongAxisCycle(0, true);
    }

    public override void CalculateLayoutInputVertical()
    {
        CalcAlongAxisCycle(1, true);
    }

    public override void SetLayoutHorizontal()
    {
        CalcCellAlongAxisCycle(0, true);
        SetChildrenAlongAxisCycle(0, true);
    }

    public override void SetLayoutVertical()
    {
        CalcCellAlongAxisCycle(1, true);
        SetChildrenAlongAxisCycle(1, true);
    }
}
