using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ContentSizeFitter))]
public abstract class HorizontalOrVerticalLayoutGroupCycle : HorizontalOrVerticalLayoutGroup, ILayoutGroupCycle
{
    public delegate void OnGetCellSize(int index, int axis, bool controlSize, bool childForceExpand,
    out float min, out float preferred, out float flexible, out float scaleFactor);

    public struct CellInfo
    {
        public Vector2 min;
        public Vector2 preferred;
        public Vector2 flexible;
        public Vector2 pos;
        public Vector2 size;
        public Vector2 scale;
    }

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
    public OnGetCellSize onGetCellSize { get; set; }

    protected int m_StartIndex = -1;
    protected int[] m_ChildIndexMap = null;
    protected Vector2 m_NormalizedPosition = Vector2.zero;
    protected CellInfo[] m_CellInfoMap = null;
    private List<GameObject> m_PendingDeactiveList = new List<GameObject>();
    private List<GameObject> m_PendingPopulateList = new List<GameObject>();

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

        if (m_ChildIndexMap != null)
        {
            for (var i = 0; i < m_ChildIndexMap.Length; ++i)
            {
                m_ChildIndexMap[i] = -1;
            }
        }

        SetDirty();
    }

    public abstract void ResetPosition();

    public abstract void Locate(uint index);

    protected void LocateAlongAxis(int axis, uint index)
    {
        if (m_CellInfoMap != null && index < m_CellInfoMap.Length)
        {
            var contentSize = rectTransform.rect.size[axis];
            var viewSize = scrollRect.viewport.rect.size[axis];
            var scrollSize = Mathf.Max(0, contentSize - viewSize);
            var viewMin = m_NormalizedPosition[axis] * scrollSize;
            var viewMax = viewMin + viewSize;
            var cellInfo = m_CellInfoMap[index];

            var normalizedPosition = scrollRect.normalizedPosition;
            if (cellInfo.pos[axis] < viewMin)
            {
                normalizedPosition[axis] = cellInfo.pos[axis] / scrollSize;
                if (axis == 1) normalizedPosition[axis] = 1 - normalizedPosition[axis];
            }
            else if (cellInfo.pos[axis] + cellInfo.size[axis] > viewMax)
            {
                normalizedPosition[axis] = (cellInfo.pos[axis] + cellInfo.size[axis] - viewSize) / scrollSize;
                if (axis == 1) normalizedPosition[axis] = 1 - normalizedPosition[axis];
            }

            scrollRect.normalizedPosition = normalizedPosition;
        }
    }

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

        if (m_CellInfoMap == null || m_CellInfoMap.Length != capacity)
        {
            m_CellInfoMap = new CellInfo[capacity];
        }
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

        for (int i = 0; i < capacity; i++)
        {
            float min, preferred, flexible, scaleFactor;
            if (onGetCellSize != null)
            {
                onGetCellSize(i, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible, out scaleFactor);
            }
            else
            {
                scaleFactor = rectChildren[0].localScale[axis];
                GetChildSizesCycle(rectChildren[0], axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
            }

            m_CellInfoMap[i].min[axis] = min;
            m_CellInfoMap[i].preferred[axis] = preferred;
            m_CellInfoMap[i].flexible[axis] = flexible;
            m_CellInfoMap[i].scale[axis] = scaleFactor;

            if (useScale)
            {
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
                totalMin += min + spacing;
                totalPreferred += preferred + spacing;

                // Increment flexible size with element's flexible size.
                totalFlexible += flexible;
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

    protected void CalcCellAlongAxisCycle(int axis, bool isVertical)
    {
        float size = rectTransform.rect.size[axis];
        bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);

        bool alongOtherAxis = (isVertical ^ (axis == 1));
        if (alongOtherAxis)
        {
            float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
            for (int i = 0; i < capacity; i++)
            {
                float requiredSpace = Mathf.Clamp(innerSize, m_CellInfoMap[i].min[axis], m_CellInfoMap[i].flexible[axis] > 0 ? size : m_CellInfoMap[i].preferred[axis]);
                float startOffset = GetStartOffset(axis, requiredSpace * (useScale ? m_CellInfoMap[i].scale[axis] : 1.0f));

                m_CellInfoMap[i].size[axis] = requiredSpace;
                m_CellInfoMap[i].pos[axis] = startOffset;
            }
        }
        else if (capacity > 0)
        {
            float pos = (axis == 0 ? padding.left : padding.top);
            float itemFlexibleMultiplier = 0;
            float surplusSpace = size - GetTotalPreferredSize(axis);

            if (surplusSpace > 0)
            {
                if (GetTotalFlexibleSize(axis) == 0)
                    pos = GetStartOffset(axis, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                else if (GetTotalFlexibleSize(axis) > 0)
                    itemFlexibleMultiplier = surplusSpace / GetTotalFlexibleSize(axis);
            }

            float minMaxLerp = 0;
            if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

            for (int i = 0; i < capacity; i++)
            {
                var index = reversed ? capacity - 1 - i : i;
                m_CellInfoMap[index].size[axis] = Mathf.Lerp(m_CellInfoMap[index].min[axis], m_CellInfoMap[index].preferred[axis], minMaxLerp);
                m_CellInfoMap[index].size[axis] += m_CellInfoMap[index].flexible[axis] * itemFlexibleMultiplier;
                m_CellInfoMap[index].pos[axis] = pos;
                pos += m_CellInfoMap[index].size[axis] * (useScale ? m_CellInfoMap[index].scale[axis] : 1.0f) + spacing;
            }
        }
    }

    /// <summary>
    /// Set the positions and sizes of the child layout elements for the given axis.
    /// </summary>
    /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
    /// <param name="isVertical">Is this group a vertical group?</param>
    protected void SetChildrenAlongAxisCycle(int axis, bool isVertical, bool cycling = false)
    {
        if (rectChildren.Count == 0)
        {
            return;
        }

        float size = rectTransform.rect.size[axis];
        bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
        bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
        float alignmentOnAxis = GetAlignmentOnAxis(axis);

        float viewSize = scrollRect.viewport.rect.size[axis];
        float scrollSize = Mathf.Max(0, size - viewSize);

        var scrollAxis = isVertical ? 1 : 0;
        float currentOffset = scrollSize * (reversed ? 1 - m_NormalizedPosition[scrollAxis] : m_NormalizedPosition[scrollAxis]);
        if (reversed)
        {
            currentOffset = size - currentOffset;
        }

        int newStartIndex = Mathf.Max(0, (int)capacity - 1);
        if (reversed)
        {
            for (; newStartIndex > 0; --newStartIndex)
            {
                // TODO: scale should be take into consideration
                if (m_CellInfoMap[newStartIndex].pos[scrollAxis] + m_CellInfoMap[newStartIndex].size[scrollAxis] >= currentOffset)
                {
                    break;
                }
            }
        }
        else
        {
            for (; newStartIndex > 0; --newStartIndex)
            {
                // TODO: scale should be take into consideration
                if (m_CellInfoMap[newStartIndex].pos[scrollAxis] <= currentOffset)
                {
                    break;
                }
            }
        }

        bool alongOtherAxis = (isVertical ^ (axis == 1));
        if (alongOtherAxis)
        {
            if (capacity > 0)
            {
                float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    var index = newStartIndex + i;
                    if (index < capacity)
                    {
                        var childIndex = index % rectChildren.Count;
                        var child = rectChildren[childIndex];
                        var scale = useScale ? m_CellInfoMap[index].scale[axis] : 1.0f;

                        if (controlSize)
                        {
                            SetChildAlongAxisWithScale(child, axis, m_CellInfoMap[index].pos[axis], m_CellInfoMap[index].size[axis], scale);
                        }
                        else
                        {
                            float offsetInCell = (m_CellInfoMap[index].size[axis] - child.sizeDelta[axis]) * alignmentOnAxis;
                            SetChildAlongAxisWithScale(child, axis, m_CellInfoMap[index].pos[axis] + offsetInCell, scale);
                        }
                    }
                }
            }
        }
        else
        {
            if (!cycling || newStartIndex != m_StartIndex)
            {
                m_StartIndex = newStartIndex;
                for (int i = 0; i < rectChildren.Count; i++)
                {
                    var index = m_StartIndex + i;
                    if (index < capacity || index < rectChildren.Count)
                    {
                        var childIndex = index % rectChildren.Count;
                        if (!cycling || m_ChildIndexMap[childIndex] != index)
                        {
                            var child = rectChildren[childIndex];

                            if (index < capacity)
                            {
                                var scale = useScale ? m_CellInfoMap[index].scale[axis] : 1.0f;
                                if (controlSize)
                                {
                                    SetChildAlongAxisWithScale(child, axis, m_CellInfoMap[index].pos[axis], m_CellInfoMap[index].size[axis], scale);
                                }
                                else
                                {
                                    float offsetInCell = (m_CellInfoMap[index].size[axis] - child.sizeDelta[axis]) * alignmentOnAxis;
                                    SetChildAlongAxisWithScale(child, axis, m_CellInfoMap[index].pos[axis] + offsetInCell, scale);
                                }
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
                                    m_PendingDeactiveList.Add(child.gameObject);
                                }
                            }

                            if (index < capacity && m_ChildIndexMap[childIndex] != index)
                            {
                                m_ChildIndexMap[childIndex] = index;

                                m_PendingPopulateList.Add(child.gameObject);
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
        if (m_PendingDeactiveList.Count > 0)
        {
            m_PendingDeactiveList.ForEach(go => go.SetActive(false));

            m_PendingDeactiveList.Clear();
        }

        if (m_PendingPopulateList.Count > 0)
        {
            m_PendingPopulateList.ForEach(go =>
            {
                var index = m_ChildIndexMap[go.transform.GetSiblingIndex()];

                onPopulateChild?.Invoke(go, index);

                Debug.Log($"HorizontalOrVerticlLayoutGroupCycle.PopulateChild({index})");
            });

            m_PendingPopulateList.Clear();
        }
    }
}
