using UnityEngine;

public interface IBuildPlacementLogic
{
    void Setup(GameObject prefab, float rotation);
    void OnStartDrag(Vector3 worldStart);
    void OnDragging(Vector3 worldCurrent);
    void OnEndDrag(Vector3 worldEnd);
}