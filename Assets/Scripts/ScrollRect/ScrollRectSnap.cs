using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectSnap : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    private bool m_Sanpping = false;
    private bool m_Dragging = false;
    private Vector2 m_SnapSrc = Vector2.zero;
    private Vector2 m_SnapDst = Vector2.zero;
    private float m_SnapTime = 0.0f;

    protected override void Awake()
    {
        Setup();
    }

    private void Setup()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (targetParent == null && scrollRect != null)
        {
            targetParent = scrollRect.content;
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (!IsActive())
            return;

        if (m_Sanpping)
        {
            m_Sanpping = false;
            m_OnEndSnap.Invoke();
        }

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
        return base.IsActive() && m_ScrollRect != null;
    }

    // for LayoutGroupCycles that doesn't have all the children RectTransform on the fly.
    public Vector2 GetRelativePosition(RectTransform reference, Rect rect, Matrix4x4 toWorldMatrix, float pivot, int offset)
    {
        var xPos = Mathf.Lerp(rect.xMin, rect.xMax, pivot) + offset;
        var yPos = Mathf.Lerp(rect.yMin, rect.yMax, pivot) + offset;
        return reference.InverseTransformPoint(toWorldMatrix.MultiplyPoint3x4(new Vector3(xPos, yPos)));
    }

    // get position after offset relative to reference RectTransform
    public Vector2 GetRelativePosition(RectTransform reference, RectTransform target, float pivot, int offset)
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

    protected void AutoSnap()
    {
        if (m_AutoSnap && !m_Sanpping && !scrollRect.content.anchoredPosition.Equals(m_SnapDst))
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
        m_SnapSrc = scrollRect.content.anchoredPosition;
        m_SnapDst = m_SnapSrc + offset;
        if (clampWithinContent)
        {
            // TODO: clamp final pos within content rect
        }
        m_SnapTime = 0.0f;
        m_Sanpping = true;
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
                // TODO: clamp snap pos within content rect.
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
                // TODO: clamp snap pos within content rect
                offset[scrollAxis] = viewSnapPos[scrollAxis] - targetSnapPos[scrollAxis];
            }
        }

        return offset;
    }

    void ProcessSnap()
    {
        if (m_Sanpping)
        {
            var scrollAxis = scrollRect.vertical ? 1 : 0;
            m_SnapTime = Math.Min(m_SnapTime + Time.unscaledDeltaTime, m_SmoothTime);

            var position = scrollRect.content.anchoredPosition;
            position[scrollAxis] = Ease.Tween(m_TweenType, m_SnapSrc[scrollAxis], m_SnapDst[scrollAxis], m_SnapTime / m_SmoothTime);
            scrollRect.content.anchoredPosition = position;

            if (m_SnapTime >= m_SmoothTime)
            {
                m_Sanpping = false;
                m_OnEndSnap.Invoke();
            }
        }
    }

    void LateUpdate()
    {
        if (scrollRect == null)
            return;

        if (m_Dragging) // skip while dragging.
            return;

        AutoSnap();

        ProcessSnap();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        Setup();

        if (m_AutoSnap)
        {
            SnapRelative(GetSnapOffsetToNearestTarget());
        }
    }
#endif
}