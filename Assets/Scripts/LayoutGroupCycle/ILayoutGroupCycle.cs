using UnityEngine;

public delegate void OnPopulateChild(GameObject gameObject, int index);

public interface ILayoutGroupCycle
{
    void SetSize(uint value);

    void Populate();

    Rect? GetCellRect(uint index);
}
