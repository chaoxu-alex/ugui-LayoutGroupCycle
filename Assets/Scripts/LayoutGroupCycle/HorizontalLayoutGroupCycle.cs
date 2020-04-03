using UnityEngine;
using UnityEngine.UI;

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

        var contentSizeFitter = GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    protected override void OnScrolling(Vector2 normalizedPosition)
    {
        m_NormalizedPosition = normalizedPosition;

        // there are chances that size and m_CellInfoMap.Length doesn't match when changing size frequently in editor.
        if (size == m_CellInfoMap.Length)
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

    public override void Locate(uint index, bool includeSpacing = true)
    {
        LocateAlongAxis(0, index, includeSpacing);
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
