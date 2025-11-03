using UnityEngine;

[ExecuteAlways]
public class GridRenderer : MonoBehaviour
{
    [SerializeField] float gridSize = 1f;
    [SerializeField] int halfExtent = 20;
    [SerializeField] Color lineColor = new Color(1f, 1f, 1f, 0.1f);

    void OnDrawGizmos()
    {
        Gizmos.color = lineColor;

        for (float x = -halfExtent; x <= halfExtent; x += gridSize)
        {
            Gizmos.DrawLine(new Vector3(x, -halfExtent, 0), new Vector3(x, halfExtent, 0));
        }
        for (float y = -halfExtent; y <= halfExtent; y += gridSize)
        {
            Gizmos.DrawLine(new Vector3(-halfExtent, y, 0), new Vector3(halfExtent, y, 0));
        }
    }
}