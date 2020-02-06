using UnityEngine;

public class HorizontalLayoutGroupCycle : HorizontalOrVerticalLayoutGroupCycle
{
    protected HorizontalLayoutGroupCycle() {}

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
        m_NormalizedPosition = reversed ? Vector2.one - normalizedPosition : normalizedPosition;

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
