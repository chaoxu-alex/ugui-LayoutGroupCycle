using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectSnap : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Serializable]
    public class BeginSnapEvent : UnityEvent { }
    [Serializable]
    public class SnapEvent : UnityEvent { }
    [Serializable]
    public class EndSnapEvent : UnityEvent { }

    [SerializeField]
    [Range(0f, 1f)]
    protected float m_ViewOffset = 0.5f;

    [SerializeField]
    [Range(0f, 1f)]
    protected float m_ChildOffset = 0.5f;

    [SerializeField]
    protected float m_SpeedThreshold = 500.0f;

    [SerializeField]
    protected float m_SmoothTime = 0.1f;

    [SerializeField]
    protected ScrollRect m_ScrollRect;
    public ScrollRect scrollRect { get { return m_ScrollRect; } set { m_ScrollRect = value; } }

    private bool m_Sanpping = false;
    private bool m_Dragging = false;
    private Vector2 m_SnapPos = Vector2.zero;

    [SerializeField]
    private BeginSnapEvent m_OnBeginSnap = new BeginSnapEvent();
    public BeginSnapEvent onBeginSnap { get { return m_OnBeginSnap; } set { m_OnBeginSnap = value; } }

    [SerializeField]
    private SnapEvent m_OnSnap = new SnapEvent();
    public SnapEvent onSnap { get { return m_OnSnap; } set { m_OnSnap = value; } }

    [SerializeField]
    private EndSnapEvent m_OnEndSnap = new EndSnapEvent();
    public EndSnapEvent onEndSnap { get { return m_OnEndSnap; } set { m_OnEndSnap = value; } }

    protected override void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        m_Sanpping = false;
        m_OnEndSnap.Invoke();
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
        if (!m_Dragging)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsActive())
            return;
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

    protected virtual Vector2 GetSnapOffset()
    {
        Vector2 offset = Vector2.zero;

        if (scrollRect != null)
        {
            var view = scrollRect.viewport;
            var viewBounds = new Bounds(view.rect.center, view.rect.size);
            var snapViewX = Mathf.Lerp(viewBounds.min.x, viewBounds.max.x, m_ViewOffset);
            var snapViewY = Mathf.Lerp(viewBounds.min.y, viewBounds.max.y, m_ViewOffset);
            var snapViewPos = new Vector3(snapViewX, snapViewY, 0.0f);
            var snapViewWorldPos = view.TransformPoint(snapViewPos);

            var content = scrollRect.content;
            var snapViewOffsetContent = content.InverseTransformPoint(snapViewWorldPos);

            var scrollAxis = scrollRect.vertical ? 1 : 0;
            var minDistance = float.MaxValue;
            foreach (RectTransform child in content)
            {
                if (child.gameObject.activeSelf)
                {
                    var snapChildX = Mathf.Lerp(child.rect.min.x, child.rect.max.x, m_ChildOffset);
                    var snapChildY = Mathf.Lerp(child.rect.min.y, child.rect.max.y, m_ChildOffset);
                    var snapChildPos = new Vector3(snapChildX, snapChildY);
                    var snapChildOffsetContent = content.InverseTransformPoint(child.TransformPoint(snapChildPos));
                    var distance = Mathf.Abs(snapViewOffsetContent[scrollAxis] - snapChildOffsetContent[scrollAxis]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        offset[scrollAxis] = snapViewOffsetContent[scrollAxis] - snapChildOffsetContent[scrollAxis];
                    }
                }
            }
        }

        return offset;
    }

    void LateUpdate()
    {
        if (scrollRect == null)
            return;

        if (m_Dragging) // skip while dragging.
            return;

        var scrollAxis = scrollRect.vertical ? 1 : 0;
        var normalizedPositionOnScrollAxis = scrollRect.normalizedPosition[scrollAxis];

        if (!m_Sanpping && !scrollRect.content.anchoredPosition.Equals(m_SnapPos))
        {
            // skip elasticity process.
            if (0.0f <= normalizedPositionOnScrollAxis && normalizedPositionOnScrollAxis <= 1.0f)
            {
                // skip inertia process until speed drops down to the threshold
                if (Mathf.Abs(scrollRect.velocity[scrollAxis]) < m_SpeedThreshold)
                {
                    scrollRect.StopMovement();
                    m_SnapPos = scrollRect.content.anchoredPosition + GetSnapOffset();
                    m_Sanpping = true;
                    m_OnBeginSnap.Invoke();
                }
            }
        }

        if (m_Sanpping)
        {
            m_OnSnap.Invoke();
            var position = scrollRect.content.anchoredPosition;
            var velocity = scrollRect.velocity;
            float speed = velocity[scrollAxis];
            position[scrollAxis] = Mathf.SmoothDamp(scrollRect.content.anchoredPosition[scrollAxis], m_SnapPos[scrollAxis], ref speed, m_SmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (Mathf.Abs(speed) < 1)
            {
                speed = 0;
                position = m_SnapPos;
                m_Sanpping = false;
                m_OnEndSnap.Invoke();
            }
            velocity[scrollAxis] = speed;

            scrollRect.velocity = velocity;
            scrollRect.content.anchoredPosition = position;
        }
    }
}