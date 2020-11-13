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
    private EndSnapEvent m_OnEndSnap = new EndSnapEvent();
    public EndSnapEvent onEndSnap { get { return m_OnEndSnap; } set { m_OnEndSnap = value; } }

    public class ScrollDragEvent : UnityEvent<PointerEventData> { }
    public ScrollDragEvent onDragEnd = new ScrollDragEvent();
    public ScrollDragEvent onDrag = new ScrollDragEvent();
    public ScrollDragEvent onDragBegin = new ScrollDragEvent();

    [SerializeField]
    protected RectTransform m_TargetParent;
    public RectTransform targetParent
    {
        get
        {
            if (m_TargetParent == null)
            {
                m_TargetParent = scrollRect.content;
            }

            return m_TargetParent;
        }
        set
        {
            m_TargetParent = value;
        }
    }

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
    protected Ease.Type m_EaseType = Ease.Type.Linear;
    public Ease.Type easeType { get { return m_EaseType; } set { m_EaseType = value; } }

    [SerializeField]
    protected AnimationCurve m_EaseCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    public AnimationCurve easeCurve { get { return m_EaseCurve; } set { m_EaseCurve = value; } }

    [SerializeField]
    protected bool m_ClampWithinContent = true;
    public bool clampWithinContent { get { return m_ClampWithinContent; } set { m_ClampWithinContent = value; } }

    [SerializeField]
    [Tooltip("Automatically snap to nearest target.")]
    protected bool m_AutoSnap = true;
    public bool autoSnap { get { return m_AutoSnap; } set { m_AutoSnap = value; } }

    [SerializeField]
    [Range(0.0f, 1.0f)]
    [Tooltip("Prediction time for calculate the inertial offset")]
    protected float m_InertialPredictTime = 0.2f;
    public float inertialPredictTime { get { return m_InertialPredictTime; } set { m_InertialPredictTime = value; } }

    protected ScrollRect m_ScrollRect;
    public ScrollRect scrollRect
    {
        get
        {
            if (m_ScrollRect == null)
            {
                m_ScrollRect = GetComponent<ScrollRect>();
                m_ScrollRect.onValueChanged.AddListener(OnScrolling);
            }

            return m_ScrollRect;
        }
    }

    private bool m_Dragging = false;
    private Coroutine m_SnapCoroutine = null;

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

        onDragBegin.Invoke(eventData);
        m_Dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!m_Dragging)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsActive())
            return;

        onDrag.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        onDragEnd.Invoke(eventData);
        m_Dragging = false;
    }

    public override bool IsActive()
    {
        return base.IsActive() && scrollRect != null;
    }

    [ContextMenu("Reset To Start")]
    public void ResetToStart()
    {
        SnapToStart(0.0f);
    }

    public void SnapToStart(float? _smoothTime = null, Ease.Type? _easeType = null)
    {
        SnapToEdge(true, _smoothTime, _easeType);
    }

    [ContextMenu("Reset To End")]
    public void ResetToEnd()
    {
        SnapToEnd(0.0f);
    }

    public void SnapToEnd(float? _smoothTime = null, Ease.Type? _easeType = null)
    {
        SnapToEdge(false, _smoothTime, _easeType);
    }

    protected void SnapToEdge(bool start, float? _smoothTime, Ease.Type? _easeType)
    {
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
        if (!start)
        {
            nPos[scrollAxis] = 1.0f - nPos[scrollAxis];
        }

        var snapViewPos = GetRelativePosition(scrollRect.content, scrollRect.viewport, nPos[scrollAxis], 0);
        var snapEdgePos = GetRelativePosition(scrollRect.content, scrollRect.content, nPos[scrollAxis], 0);
        SnapRelative(snapViewPos - snapEdgePos, _smoothTime, _easeType);
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
    protected Vector2 GetSnapOffsetToNearestTarget(Vector2 inertiaOffset)
    {
        Vector2 result = Vector2.zero;

        // cache the original position of content
        var originalPos = scrollRect.content.anchoredPosition;
        // translate content by inertial offset since the calculation below should be based on predicted result with inertial offset
        scrollRect.content.anchoredPosition = scrollRect.content.anchoredPosition + inertiaOffset;

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

        // reset content to cached original position as inertial offset is just a prediction.
        scrollRect.content.anchoredPosition = originalPos;

        // add inertial offset to final result as the process above ignores it when calculating the snap offset
        return result + inertiaOffset;
    }

    protected void OnScrolling(Vector2 nPos)
    {
        if (autoSnap && !m_Dragging && m_SnapCoroutine == null && gameObject.activeInHierarchy)
        {
            var scrollAxis = scrollRect.vertical ? 1 : 0;
            var normalizedPositionOnScrollAxis = scrollRect.normalizedPosition[scrollAxis];
            var isElasticProcessing = scrollRect.movementType == ScrollRect.MovementType.Elastic && !(-0.001f < normalizedPositionOnScrollAxis && normalizedPositionOnScrollAxis < 1.001f);
            if (!isElasticProcessing)
            {
                var inertiaOffset = Vector2.zero;
                if (Mathf.Abs(scrollRect.velocity[scrollAxis]) > 0 && scrollRect.inertia && scrollRect.decelerationRate > 0.0f)
                {
                    var velocity = scrollRect.velocity * Mathf.Pow(scrollRect.decelerationRate, m_InertialPredictTime);
                    inertiaOffset[scrollAxis] = (scrollRect.velocity[scrollAxis] + velocity[scrollAxis]) / 2 * m_InertialPredictTime;
                    scrollRect.StopMovement();
                }
                SnapRelative(GetSnapOffsetToNearestTarget(inertiaOffset));
            }
        }
    }

    public void SnapRelative(Vector2 offset, float? _smoothTime = null, Ease.Type? _easeType = null)
    {
        if (gameObject.activeInHierarchy)
        {
            scrollRect.StopMovement();

            StopSnap();

            if (clampWithinContent)
            {
                offset = ClampSnapOffset(offset);
            }

            var time = _smoothTime != null ? _smoothTime.Value : smoothTime;
            var type = _easeType != null ? _easeType.Value : easeType;
            StartCoroutine(ToggleAutoSnap());

            m_SnapCoroutine = StartCoroutine(SnapCoroutine(offset, time, type));
        }
    }

    // snap position will trigger ScrollRect.onValueChange in LateUpdate of this frame, which will
    // trigger AutoSnap right away, cells's position may not get updated this moment, the final snap
    // pos will be incorrect. so here autoSnap will be disabled first and recovered later after two
    // frames. (one frame delay doesn't make this right)
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

    protected IEnumerator SnapCoroutine(Vector2 offset, float time, Ease.Type easeType)
    {
        float elapsed = 0.0f;
        var scrollAxis = scrollRect.vertical ? 1 : 0;
        var startPos = scrollRect.content.anchoredPosition;
        var endPos = startPos + offset;

        while (elapsed < time)
        {
            elapsed = Math.Min(elapsed + Time.unscaledDeltaTime, time);
            var progress = elapsed / time;
            var position = scrollRect.content.anchoredPosition;
            if (m_EaseType == Ease.Type.Custom)
            {
                position[scrollAxis] = Mathf.Lerp(startPos[scrollAxis], endPos[scrollAxis], m_EaseCurve != null ? m_EaseCurve.Evaluate(progress) : progress);
            }
            else
            {
                position[scrollAxis] = Ease.Evaluate(m_EaseType, startPos[scrollAxis], endPos[scrollAxis], progress);
            }
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

    public void SnapTo(uint index, float? _smoothTime = null, Ease.Type? _easeType = null)
    {
        SnapRelative(GetSnapOffsetByIndex(index), _smoothTime, _easeType);
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

    // #if UNITY_EDITOR
    //     protected override void OnValidate()
    //     {
    //         base.OnValidate();

    //            if (autoSnap && gameObject.activeInHierarchy)
    //            {
    //                SnapRelative(GetSnapOffsetToNearestTarget());
    //            }
    //     }
    // #endif
}