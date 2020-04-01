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
    protected ScrollRect m_ScrollRect;
    public ScrollRect scrollRect { get { return m_ScrollRect; } set { m_ScrollRect = value; } }

    [SerializeField]
    protected RectTransform m_ChildrenRoot;
    public RectTransform childrenRoot { get { return m_ChildrenRoot; } set { m_ChildrenRoot = value; } }

    [SerializeField]
    [Range(0f, 1f)]
    protected float m_ViewOffset = 0.5f;
    public float viewOffset { get { return m_ViewOffset; } set { m_ViewOffset = Mathf.Clamp01(value); } }

    [SerializeField]
    protected int m_ViewOffsetPixel = 0;
    public int viewOffsetPixel { get { return m_ViewOffsetPixel; } set { m_ViewOffsetPixel = value; } }

    [SerializeField]
    [Range(0f, 1f)]
    protected float m_ChildOffset = 0.5f;
    public float childOffset { get { return m_ChildOffset; } set { m_ChildOffset = Mathf.Clamp01(value); } }

    [SerializeField]
    protected int m_ChildOffsetPixel = 0;
    public int childOffsetPixel { get { return m_ChildOffsetPixel; } set { m_ChildOffsetPixel = value; } }

    [SerializeField]
    protected float m_SpeedThreshold = 500.0f;
    public float speedThreshold { get { return m_SpeedThreshold; } set { m_SpeedThreshold = value; } }

    [SerializeField]
    protected float m_SmoothTime = 0.1f;
    public float smoothTime { get { return m_SmoothTime; } set { m_SmoothTime = value; } }

    [SerializeField]
    private BeginSnapEvent m_OnBeginSnap = new BeginSnapEvent();
    public BeginSnapEvent onBeginSnap { get { return m_OnBeginSnap; } set { m_OnBeginSnap = value; } }

    [SerializeField]
    private SnapEvent m_OnSnap = new SnapEvent();
    public SnapEvent onSnap { get { return m_OnSnap; } set { m_OnSnap = value; } }

    [SerializeField]
    private EndSnapEvent m_OnEndSnap = new EndSnapEvent();
    public EndSnapEvent onEndSnap { get { return m_OnEndSnap; } set { m_OnEndSnap = value; } }

    private bool m_Sanpping = false;
    private bool m_Dragging = false;
    private Vector2 m_SnapPos = Vector2.zero;

    protected override void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        if (childrenRoot == null && scrollRect != null)
        {
            childrenRoot = scrollRect.content;
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

    protected virtual Vector2 GetSnapOffset()
    {
        Vector2 offset = Vector2.zero;

        if (scrollRect != null)
        {
            var scrollAxis = scrollRect.vertical ? 1 : 0;

            var view = scrollRect.viewport;
            var viewBounds = new Bounds(view.rect.center, view.rect.size);
            var snapViewX = Mathf.Lerp(viewBounds.min.x, viewBounds.max.x, m_ViewOffset);
            var snapViewY = Mathf.Lerp(viewBounds.min.y, viewBounds.max.y, m_ViewOffset);
            var snapViewPos = new Vector3(snapViewX, snapViewY, 0.0f);
            var snapViewWorldPos = view.TransformPoint(snapViewPos);

            var content = scrollRect.content;
            var snapViewOffsetContent = content.InverseTransformPoint(snapViewWorldPos);
            snapViewOffsetContent[scrollAxis] += viewOffsetPixel;

            var minDistance = float.MaxValue;
            foreach (RectTransform child in childrenRoot ?? content)
            {
                if (child.gameObject.activeSelf)
                {
                    var snapChildX = Mathf.Lerp(child.rect.min.x, child.rect.max.x, m_ChildOffset);
                    var snapChildY = Mathf.Lerp(child.rect.min.y, child.rect.max.y, m_ChildOffset);
                    var snapChildPos = new Vector3(snapChildX, snapChildY);
                    var snapChildOffsetContent = content.InverseTransformPoint(child.TransformPoint(snapChildPos));
                    snapChildOffsetContent[scrollAxis] += childOffsetPixel;
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

    void PrepareSnap()
    {
        if (!m_Sanpping && !scrollRect.content.anchoredPosition.Equals(m_SnapPos))
        {
            var scrollAxis = scrollRect.vertical ? 1 : 0;
            var normalizedPositionOnScrollAxis = scrollRect.normalizedPosition[scrollAxis];
            // skip elasticity process.
            if (0.0f <= normalizedPositionOnScrollAxis && normalizedPositionOnScrollAxis <= 1.0f)
            {
                // skip inertia process until speed drops down to the threshold
                if (Mathf.Abs(scrollRect.velocity[scrollAxis]) < m_SpeedThreshold)
                {
                    // scrollRect.StopMovement();
                    // TODO: clamp final pos within content bounds
                    m_SnapPos = scrollRect.content.anchoredPosition + GetSnapOffset();
                    m_Sanpping = true;
                    m_OnBeginSnap.Invoke();
                }
            }
        }
    }

    void UpdateSnap()
    {
        if (m_Sanpping)
        {
            m_OnSnap.Invoke();

            var scrollAxis = scrollRect.vertical ? 1 : 0;
            var velocity = scrollRect.velocity;
            float speed = velocity[scrollAxis];

            var position = scrollRect.content.anchoredPosition;
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

    void LateUpdate()
    {
        if (scrollRect == null)
            return;

        if (m_Dragging) // skip while dragging.
            return;

        PrepareSnap();

        UpdateSnap();
    }
}