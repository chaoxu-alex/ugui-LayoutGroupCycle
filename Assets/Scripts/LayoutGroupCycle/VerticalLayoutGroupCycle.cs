﻿using UnityEngine;
using UnityEngine.UI;

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

        var contentSizeFitter = GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    protected override void OnScrolling(Vector2 normalizedPosition)
    {
        // vertical layout group cycle is aligned from top to bottom by default while y axis growth from bottom to top.
        m_NormalizedPosition = Vector2.one - normalizedPosition;

        // there are chances that size and m_CellInfoMap.Length doesn't match when changing size frequently in editor.
        if (size == m_CellInfoMap.Length)
        {
            SetChildrenAlongAxisCycle(0, true, true);

            SetChildrenAlongAxisCycle(1, true, true);
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
