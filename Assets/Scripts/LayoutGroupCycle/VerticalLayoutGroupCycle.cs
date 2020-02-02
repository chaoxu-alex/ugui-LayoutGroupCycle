using UnityEngine;

public class VerticalLayoutGroupCycle : HorizontalOrVerticalLayoutGroupCycle
{
    protected VerticalLayoutGroupCycle() {}

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
        m_NormalizedPosition = reversed ? normalizedPosition : Vector2.one - normalizedPosition;

        SetChildrenAlongAxisCycle(0, true, true);

        SetChildrenAlongAxisCycle(1, true, true);
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
        SetChildrenAlongAxisCycle(0, true);
    }

    public override void SetLayoutVertical()
    {
        SetChildrenAlongAxisCycle(1, true);
    }
}
