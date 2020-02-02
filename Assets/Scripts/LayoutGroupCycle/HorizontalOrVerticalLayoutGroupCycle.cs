using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public abstract class HorizontalOrVerticalLayoutGroupCycle : HorizontalOrVerticalLayoutGroup, ILayoutGroupCycle
{
    [SerializeField]
    protected ScrollRect m_ScrollRect;
    public ScrollRect scrollRect { get { return m_ScrollRect; } set { m_ScrollRect = value; } }

    [SerializeField]
    protected uint m_Capacity;
    public uint capacity { get { return m_Capacity; } set { SetProperty(ref m_Capacity, value); } }
    [SerializeField]
    protected bool m_Reversed = false;
    public bool reversed { get { return m_Reversed; } set { SetProperty(ref m_Reversed, value); } }

    public OnPopulateChild onPopulateChild { get; set; }

    protected Vector2 m_NormalizedPosition = Vector2.zero;
    private int m_StartIndex = -1;
    private int[] m_ChildIndexMap = null;
    private List<GameObject> m_PendingDeactiveList = null;
    private List<GameObject> m_PendingPopulateList = null;

    protected override void Awake()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(OnScrolling);
        }
        else
        {
            Debug.LogError("HorizontalOrVerticalLayoutGroupCycle should be used with ScrollRect.");
        }
    }

    protected abstract void OnScrolling(Vector2 normalizedPosition);

    // this function makes sure Populate() is called while property capacity doesn't when capacity stay the same
    public void SetCapacity(uint value)
    {
        capacity = value;

        Populate();
    }

    [ContextMenu("Populate")]
    public void Populate()
    {
        m_StartIndex = -1;
        m_ChildIndexMap = Enumerable.Repeat(-1, rectChildren.Count).ToArray();

        SetDirty();
    }

    public abstract void ResetPosition();

    public override void CalculateLayoutInputHorizontal()
    {
        rectChildren.Clear();
        var toIgnoreList = ListPool<Component>.Get();
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            var rect = rectTransform.GetChild(i) as RectTransform;
            if (rect == null)
                continue;

            rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

            if (toIgnoreList.Count == 0)
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

        if (m_ChildIndexMap == null || m_ChildIndexMap.Length != rectChildren.Count)
        {
            m_ChildIndexMap = Enumerable.Repeat(-1, rectChildren.Count).ToArray();
        }

        m_Tracker.Clear();
    }

    /// <summary>
    /// Calculate the layout element properties for this layout element along the given axis.
    /// </summary>
    /// <param name="axis">The axis to calculate for. 0 is horizontal and 1 is vertical.</param>
    /// <param name="isVertical">Is this group a vertical group?</param>
    protected void CalcAlongAxisCycle(int axis, bool isVertical)
    {
        float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
        bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
        bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
        bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);

        float totalMin = combinedPadding;
        float totalPreferred = combinedPadding;
        float totalFlexible = 0;

        bool alongOtherAxis = (isVertical ^ (axis == 1));

        if (rectChildren.Count > 0)
        {
            RectTransform child = rectChildren[0];
            float min, preferred, flexible;
            GetChildSizesCycle(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);

            if (useScale)
            {
                float scaleFactor = child.localScale[axis];
                min *= scaleFactor;
                preferred *= scaleFactor;
                flexible *= scaleFactor;
            }

            if (alongOtherAxis)
            {
                totalMin = Mathf.Max(min + combinedPadding, totalMin);
                totalPreferred = Mathf.Max(preferred + combinedPadding, totalPreferred);
                totalFlexible = Mathf.Max(flexible, totalFlexible);
            }
            else
            {
                totalMin += (min + spacing) * capacity;
                totalPreferred += (preferred + spacing) * capacity;

                // Increment flexible size with element's flexible size.
                totalFlexible += flexible * capacity;
            }
        }

        if (!alongOtherAxis && rectChildren.Count > 0)
        {
            totalMin -= spacing;
            totalPreferred -= spacing;
        }
        totalPreferred = Mathf.Max(totalMin, totalPreferred);
        SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
    }

    /// <summary>
    /// Set the positions and sizes of the child layout elements for the given axis.
    /// </summary>
    /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
    /// <param name="isVertical">Is this group a vertical group?</param>
    protected void SetChildrenAlongAxisCycle(int axis, bool isVertical, bool cycling = false)
    {
        float size = rectTransform.rect.size[axis];
        bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
        bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
        bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
        float alignmentOnAxis = GetAlignmentOnAxis(axis);

        bool alongOtherAxis = (isVertical ^ (axis == 1));
        if (alongOtherAxis)
        {
            float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
            for (int i = 0; i < rectChildren.Count; i++)
            {
                RectTransform child = rectChildren[i];
                float min, preferred, flexible;
                GetChildSizesCycle(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                float scaleFactor = useScale ? child.localScale[axis] : 1f;

                float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                float startOffset = GetStartOffset(axis, requiredSpace * scaleFactor);
                if (controlSize)
                {
                    SetChildAlongAxisWithScale(child, axis, startOffset, requiredSpace, scaleFactor);
                }
                else
                {
                    float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                    SetChildAlongAxisWithScale(child, axis, startOffset + offsetInCell, scaleFactor);
                }
            }
        }
        else if (rectChildren.Count > 0)
        {
            float startPos = (axis == 0 ? padding.left : padding.top);
            float itemFlexibleMultiplier = 0;
            float surplusSpace = size - GetTotalPreferredSize(axis);

            if (surplusSpace > 0)
            {
                if (GetTotalFlexibleSize(axis) == 0)
                    startPos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                else if (GetTotalFlexibleSize(axis) > 0)
                    itemFlexibleMultiplier = surplusSpace / GetTotalFlexibleSize(axis);
            }

            float minMaxLerp = 0;
            if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));


            RectTransform firstChild = rectChildren[0];
            float min, preferred, flexible;
            GetChildSizesCycle(firstChild, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
            float scaleFactor = useScale ? firstChild.localScale[axis] : 1f;
            float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
            childSize += flexible * itemFlexibleMultiplier;

            float viewSize = scrollRect.viewport.rect.size[axis];
            float scrollSize = Mathf.Max(0, size - viewSize);

            float currentOffset = scrollSize * m_NormalizedPosition[axis];
            int newStartIndex = Mathf.FloorToInt(Mathf.Max(0, currentOffset - startPos) / (childSize + spacing));

            if (!cycling || newStartIndex != m_StartIndex)
            {
                m_StartIndex = newStartIndex;

                for (int i = 0; i < rectChildren.Count; i++)
                {
                    var index = i + m_StartIndex;

                    if (index < capacity || index < rectChildren.Count)
                    {
                        var childIndex = index % rectChildren.Count;

                        if (!cycling || m_ChildIndexMap[childIndex] != index)
                        {
                            var child = rectChildren[childIndex];

                            var positionIndex = reversed ? capacity - 1 - index : index;

                            float pos = startPos + (childSize * scaleFactor + spacing) * positionIndex;
                            if (controlSize)
                            {
                                SetChildAlongAxisWithScale(child, axis, pos, childSize, scaleFactor);
                            }
                            else
                            {
                                float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                                SetChildAlongAxisWithScale(child, axis, pos + offsetInCell, scaleFactor);
                            }

                            if (child.gameObject.activeSelf != index < capacity)
                            {
                                // the code commented below will trigger error while rebuilding layouts: Trying to remove xxx from rebuild list while we are already inside a rebuild loop
                                // child.gameObject.SetActive(index < capacity);
                                if (index < capacity)
                                {
                                    child.gameObject.SetActive(true);
                                }
                                else
                                {
                                    // pending deactivation of children to late update to avoid error metioned above
                                    if (m_PendingDeactiveList == null)
                                    {
                                        m_PendingDeactiveList = ListPool<GameObject>.Get();
                                    }

                                    m_PendingDeactiveList.Add(child.gameObject);
                                }
                            }

                            if (index < capacity)
                            {
                                if (m_ChildIndexMap[childIndex] != index)
                                {
                                    m_ChildIndexMap[childIndex] = index;

                                    if (m_PendingPopulateList == null)
                                    {
                                        m_PendingPopulateList = ListPool<GameObject>.Get();
                                    }

                                    m_PendingPopulateList.Add(child.gameObject);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    protected void GetChildSizesCycle(RectTransform child, int axis, bool controlSize, bool childForceExpand,
    out float min, out float preferred, out float flexible)
    {
        if (!controlSize)
        {
            min = child.sizeDelta[axis];
            preferred = min;
            flexible = 0;
        }
        else
        {
            min = LayoutUtility.GetMinSize(child, axis);
            preferred = LayoutUtility.GetPreferredSize(child, axis);
            flexible = LayoutUtility.GetFlexibleSize(child, axis);
        }

        if (childForceExpand)
        {
            flexible = Mathf.Max(flexible, 1);
        }
    }

    void LateUpdate()
    {
        if (m_PendingDeactiveList != null)
        {
            foreach (var go in m_PendingDeactiveList)
            {
                go.SetActive(false);
            }

            ListPool<GameObject>.Release(m_PendingDeactiveList);

            m_PendingDeactiveList = null;
        }

        if (m_PendingPopulateList != null)
        {
            foreach (var go in m_PendingPopulateList)
            {
                var index = m_ChildIndexMap[go.transform.GetSiblingIndex()];

                onPopulateChild?.Invoke(go, index);

                Debug.Log($"HorizontalOrVerticlLayoutGroupCycle.PopulateChild({index})");
            }

            ListPool<GameObject>.Release(m_PendingPopulateList);

            m_PendingPopulateList = null;
        }
    }
}
