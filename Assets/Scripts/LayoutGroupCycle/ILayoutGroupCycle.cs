using UnityEngine;

public delegate void OnPopulateChild(GameObject gameObject, int index);

public interface ILayoutGroupCycle
{
    void SetCapacity(uint value);

    void Populate();

    void ResetPosition();
}
