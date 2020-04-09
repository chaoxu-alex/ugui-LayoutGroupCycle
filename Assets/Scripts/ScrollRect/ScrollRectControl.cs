using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectControl : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Serializable]
    public class EndSnapEvent : UnityEvent { }

    [SerializeField]
    protected ScrollRect m_ScrollRect;
    public ScrollRect scrollRect { get { return m_ScrollRect; } set { m_ScrollRect = value; } }

    [SerializeField]
    protected RectTransform m_TargetParent;
    public RectTransform targetParent { get { return m_TargetParent; } set { m_TargetParent = value; } }

    [SerializeField]
    [Range(0f, 1f)]
    protected float m_ViewSnapPivot = 0.5f;
    public float viewSnapPivot { get { return m_ViewSnapPivot; } set { m_ViewSnapPivot = Mathf.Clamp01(value); } }

    [SerializeField]
    [Tooltip("Offet in pixel relative to view snap pivot.")]
    protected int m_ViewSnapOffset = 0;
    public int viewSnapOffset { get { return m_ViewSnapOffset; } set { m_ViewSnapOffset = value; } }

    [SerializeField]
    [Range(0f, 1f)]
    protected float m_TargetSnapPivot = 0.5f;
    public float targetSnapPivot { get { return m_TargetSnapPivot; } set { m_TargetSnapPivot = Mathf.Clamp01(value); } }

    [SerializeField]
    [Tooltip("Offet in pixel relative to target snap pivot.")]
    protected int m_TargetSnapOffset = 0;
    public int targetSnapOffset { get { return m_TargetSnapOffset; } set { m_TargetSnapOffset = value; } }

    [SerializeField]
    protected float m_SmoothTime = 0.1f;
    public float smoothTime { get { return m_SmoothTime; } set { m_SmoothTime = value; } }

    [SerializeField]
    protected Ease.TweenType m_TweenType = Ease.TweenType.Linear;
    public Ease.TweenType tweenType { get { return m_TweenType; } set { m_TweenType = value; } }

    [SerializeField]
    protected bool m_ClampWithinContent = true;
    public bool clampWithinContent { get { return m_ClampWithinContent; } set { m_ClampWithinContent = value; } }

    [SerializeField]
    [Tooltip("Automatically snap to nearest target.")]
    protected bool m_AutoSnap = true;
    public bool autoSnap { get { return m_AutoSnap; } set { m_AutoSnap = value; } }

    [SerializeField]
    [Tooltip("ScrollRect speed underwhich which snap will cut in.")]
    protected float m_CutInSpeed = 500.0f;
    public float cutInSpeed { get { return m_CutInSpeed; } set { m_CutInSpeed = value; } }

    [SerializeField]
    private EndSnapEvent m_OnEndSnap = new EndSnapEvent();
    public EndSnapEvent onEndSnap { get { return m_OnEndSnap; } set { m_OnEndSnap = value; } }

    private bool m_Dragging = false;
    private Coroutine m_SnapCoroutine = null;

    protected override void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrolling);
            if (targetParent == null)
            {
                targetParent = scrollRect.content;
            }
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (!IsActive())
            return;

        StopSnap();

        m_Dragging = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsActive())
            return;

        m_Dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // if (!m_Dragging)
        //     return;

        // if (eventData.button != PointerEventData.InputButton.Left)
        //     return;

        // if (!IsActive())
        //     return;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        m_Dragging = false;
    }

    public override bool IsActive()
    {
        return base.IsActive() && scrollRect != null;
    }

    [ContextMenu("Reset To Start")]
    public void ResetToStart()
    {
        ResetPosition();
    }

    [ContextMenu("Reset To End")]
    public void ResetToEnd()
    {
        ResetPosition(true);
    }

    // reset position to begin or end (reversed)
    public void ResetPosition(bool reversed = false)
    {
        scrollRect.StopMovement();
        StopSnap();

        var nPos = Vector2.zero;
        var parent = targetParent ?? scrollRect.content;
        while (parent != null)
        {
            var gridLayoutGroup = parent.GetComponent<GridLayoutGroup>();
            if (gridLayoutGroup != null)
            {
                // begin is defined by start corner
                var corner = new Vector2Int((int)gridLayoutGroup.startCorner % 2, (int)gridLayoutGroup.startCorner / 2);

                if (scrollRect.horizontal)
                {
                    nPos.x = corner.x == 0 ? 0.0f : 1.0f;
                }
                else if (scrollRect.vertical)
                {
                    nPos.y = corner.y == 1 ? 0.0f : 1.0f;
                }
                break;
            }

            var verticalLayoutGroup = parent.GetComponent<VerticalLayoutGroup>();
            if (verticalLayoutGroup != null)
            {
                // vertical layout group grows from top to bottom which is the reverse of y-axis
                if (scrollRect.vertical)
                {
                    nPos.y = 1.0f - nPos.y;
                }
                break;
            }

            var horizontalOrVerticalLayoutGroupCycle = parent.GetComponent<HorizontalOrVerticalLayoutGroupCycle>();
            if (horizontalOrVerticalLayoutGroupCycle != null)
            {
                if (horizontalOrVerticalLayoutGroupCycle is HorizontalLayoutGroupCycle)
                {
                    if (scrollRect.horizontal && horizontalOrVerticalLayoutGroupCycle.reversed)
                    {
                        nPos.x = 1.0f - nPos.x;
                    }
                }
                else
                {
                    // vertical layout group grows from top to bottom which is the reverse of y-axis
                    if (scrollRect.vertical)
                    {
                        nPos.y = 1.0f - nPos.y;
                        if (horizontalOrVerticalLayoutGroupCycle.reversed)
                        {
                            nPos.y = 1.0f - nPos.y;
                        }
                    }
                }

                break;
            }

            break;
        }

        var scrollAxis = scrollRect.vertical ? 1 : 0;
        if (reversed)
        {
            nPos[scrollAxis] = 1.0f - nPos[scrollAxis];
        }

        StartCoroutine(ToggleAutoSnap());
        scrollRect.normalizedPosition = nPos;
    }

    // set the normailized position will trigger ScrollRect.onValueChange in LateUpdate of this frame,
    // which will trigger AutoSnap but at that time, cells are not updated yet and not in correct position
    // yet, so the final position will be incorrect. so we disable autoSnap first and revert it after
    // scrolling stopped.
    protected IEnumerator ToggleAutoSnap()
    {
        if (autoSnap)
        {
            autoSnap = false;
            yield return null;
            yield return null; // two frames required.
            autoSnap = true;
        }
    }

    // for LayoutGroupCycles that doesn't have all the children RectTransform on the fly.
    protected Vector2 GetRelativePosition(RectTransform reference, Rect rect, Matrix4x4 toWorldMatrix, float pivot, int offset)
    {
        var xPos = Mathf.Lerp(rect.xMin, rect.xMax, pivot) + offset;
        var yPos = Mathf.Lerp(rect.yMin, rect.yMax, pivot) + offset;
        return reference.InverseTransformPoint(toWorldMatrix.MultiplyPoint3x4(new Vector3(xPos, yPos)));
    }

    // get position after offset relative to reference RectTransform
    protected Vector2 GetRelativePosition(RectTransform reference, RectTransform target, float pivot, int offset)
    {
        var xPos = Mathf.Lerp(target.rect.xMin, target.rect.xMax, pivot) + offset;
        var yPos = Mathf.Lerp(target.rect.yMin, target.rect.yMax, pivot) + offset;
        return reference.InverseTransformPoint(target.TransformPoint(new Vector3(xPos, yPos)));
    }

    // get snap offset of the nearest target under targetParent/scrollRect.content
    protected Vector2 GetSnapOffsetToNearestTarget()
    {
        Vector2 result = Vector2.zero;

        Vector2 viewSnapPos = GetRelativePosition(scrollRect.content, scrollRect.viewport, viewSnapPivot, viewSnapOffset);
        var minDistance = float.MaxValue;
        var scrollAxis = scrollRect.vertical ? 1 : 0;
        foreach (RectTransform target in targetParent ?? scrollRect.content)
        {
            if (target.gameObject.activeSelf)
            {
                var targetSnapPos = GetRelativePosition(scrollRect.content, target, targetSnapPivot, targetSnapOffset);

                var offset = viewSnapPos[scrollAxis] - targetSnapPos[scrollAxis];
                var distance = Mathf.Abs(offset);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    result[scrollAxis] = offset;
                }
            }
        }

        // no active child was found under targetParent/scrollRect.content, fallback to targetParent/scrollRect.content
        if (minDistance >= float.MaxValue)
        {
            var targetSnapPos = GetRelativePosition(scrollRect.content, targetParent ?? scrollRect.content, targetSnapPivot, targetSnapOffset);
            result[scrollAxis] = viewSnapPos[scrollAxis] - targetSnapPos[scrollAxis];
        }

        return result;
    }

    protected void OnScrolling(Vector2 nPos)
    {
        if (autoSnap && !m_Dragging && m_SnapCoroutine == null)
        {
            var scrollAxis = scrollRect.vertical ? 1 : 0;
            var normalizedPositionOnScrollAxis = scrollRect.normalizedPosition[scrollAxis];
            var isNotElasticProcessing = scrollRect.movementType != ScrollRect.MovementType.Elastic || 0.0f <= normalizedPositionOnScrollAxis && normalizedPositionOnScrollAxis <= 1.0f;
            // // skip elasticity process; skip inertia process until speed drops down to the threshold
            if (isNotElasticProcessing && Mathf.Abs(scrollRect.velocity[scrollAxis]) < m_CutInSpeed)
            {
                SnapRelative(GetSnapOffsetToNearestTarget());
            }
        }
    }

    public void SnapRelative(Vector2 offset)
    {
        scrollRect.StopMovement();

        StopSnap();

        if (clampWithinContent)
        {
            offset = ClampSnapOffset(offset);
        }

        m_SnapCoroutine = StartCoroutine(SnapCoroutine(offset, m_SmoothTime, tweenType));
    }

    protected IEnumerator SnapCoroutine(Vector2 offset, float time, Ease.TweenType tweenType)
    {
        float elapsed = 0.0f;
        var scrollAxis = scrollRect.vertical ? 1 : 0;
        var startPos = scrollRect.content.anchoredPosition;
        var endPos = startPos + offset;

        while (elapsed < time)
        {
            elapsed = Math.Min(elapsed + Time.unscaledDeltaTime, time);
            var position = scrollRect.content.anchoredPosition;
            position[scrollAxis] = Ease.Tween(m_TweenType, startPos[scrollAxis], endPos[scrollAxis], elapsed / time);
            scrollRect.content.anchoredPosition = position;
            yield return null;
        }

        scrollRect.content.anchoredPosition = endPos;
        StopSnap();
    }

    protected void StopSnap()
    {
        if (m_SnapCoroutine != null)
        {
            StopCoroutine(m_SnapCoroutine);
            m_SnapCoroutine = null;
            m_OnEndSnap.Invoke(); // TODO: a parameter indicate whether snap is ended automatically or manually.
        }
    }

    protected Vector2 ClampSnapOffset(Vector2 offset)
    {
        var contentRect = scrollRect.content.rect;
        contentRect.position += offset;
        var minInViewRect = scrollRect.viewport.InverseTransformPoint(scrollRect.content.TransformPoint(contentRect.min));
        var maxInViewRect = scrollRect.viewport.InverseTransformPoint(scrollRect.content.TransformPoint(contentRect.max));

        var fixOffset = Vector2.zero;
        var viewRect = scrollRect.viewport.rect;
        if (minInViewRect.x > viewRect.min.x && maxInViewRect.x > viewRect.max.x)
        {
            fixOffset.x = Mathf.Max(viewRect.min.x - minInViewRect.x, viewRect.max.x - maxInViewRect.x);
        }
        else if (minInViewRect.x < viewRect.min.x && maxInViewRect.x < viewRect.max.x)
        {
            fixOffset.x = Mathf.Min(viewRect.min.x - minInViewRect.x, viewRect.max.x - maxInViewRect.x);
        }

        if (minInViewRect.y > viewRect.min.y && maxInViewRect.y > viewRect.max.y)
        {
            fixOffset.y = Mathf.Max(viewRect.min.y - minInViewRect.y, viewRect.max.y - maxInViewRect.y);
        }
        else if (minInViewRect.y < viewRect.min.y && maxInViewRect.y < viewRect.max.y)
        {
            fixOffset.y = Mathf.Min(viewRect.min.y - minInViewRect.y, viewRect.max.y - maxInViewRect.y);
        }
        offset += fixOffset;

        return offset;
    }

    public void SnapTo(uint index)
    {
        SnapRelative(GetSnapOffsetByIndex(index));
    }

    protected Vector2 GetSnapOffsetByIndex(uint index)
    {
        var offset = Vector2.zero;
        var scrollAxis = scrollRect.vertical ? 1 : 0;
        Vector2 viewSnapPos = GetRelativePosition(scrollRect.content, scrollRect.viewport, viewSnapPivot, viewSnapOffset);
        var parent = targetParent ?? scrollRect.content;
        var layoutGroupCycle = parent.GetComponent<ILayoutGroupCycle>();
        if (layoutGroupCycle != null)
        {
            Rect? cellRect = layoutGroupCycle.GetCellRect(index);
            if (cellRect != null)
            {
                var targetSnapPos = GetRelativePosition(scrollRect.content, cellRect.Value, parent.localToWorldMatrix, targetSnapPivot, targetSnapOffset);
                offset[scrollAxis] = viewSnapPos[scrollAxis] - targetSnapPos[scrollAxis];
            }
        }
        else
        {
            var layoutGroup = parent.GetComponent<LayoutGroup>();
            var rectChildren = new List<RectTransform>();
            var toIgnoreList = ListPool<Component>.Get();
            for (int i = 0; i < parent.childCount; i++)
            {
                var rect = parent.GetChild(i) as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                if (layoutGroup != null)
                {
                    rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);
                }

                if (layoutGroup == null || toIgnoreList.Count == 0)
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

            if (index < rectChildren.Count)
            {
                var target = rectChildren[(int)index];
                var targetSnapPos = GetRelativePosition(scrollRect.content, target, targetSnapPivot, targetSnapOffset);
                offset[scrollAxis] = viewSnapPos[scrollAxis] - targetSnapPos[scrollAxis];
            }
        }

        return offset;
    }

//#if UNITY_EDITOR
//    protected override void OnValidate()
//    {
//        base.OnValidate();

//        if (autoSnap && gameObject.activeInHierarchy)
//        {
//            SnapRelative(GetSnapOffsetToNearestTarget());
//        }
//    }
//#endif
}