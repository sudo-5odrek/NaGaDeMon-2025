using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    // 8-directional neighbor offsets
    static readonly Vector2Int[] directions =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),      // cardinal
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)     // diagonals
    };

    // Allow or forbid moving diagonally between obstacles
    public static bool allowCornerCutting = false;

    public static List<Node> FindPath(Node start, Node goal)
    {
        if (start == null || goal == null || !goal.walkable) return null;

        var open = new List<Node> { start };
        var closed = new HashSet<Node>();

        start.g = 0;
        start.h = Heuristic(start, goal);
        start.parent = null;

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.f.CompareTo(b.f));
            var current = open[0];
            open.RemoveAt(0);
            closed.Add(current);

            if (current == goal)
                return Reconstruct(current);

            foreach (var dir in directions)
            {
                var n = GridManager.I.GetNode(current.x + dir.x, current.y + dir.y);
                if (n == null || !n.walkable || closed.Contains(n)) continue;

                // --- prevent diagonal corner cutting ---
                if (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) == 2 && !allowCornerCutting)
                {
                    // If moving diagonally, both side cells must be walkable
                    Node sideA = GridManager.I.GetNode(current.x + dir.x, current.y);
                    Node sideB = GridManager.I.GetNode(current.x, current.y + dir.y);
                    if ((sideA != null && !sideA.walkable) || (sideB != null && !sideB.walkable))
                        continue;
                }

                // --- calculate cost (√2 for diagonal, 1 for straight) ---
                float stepCost = (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) == 2) ? 1.4142f : 1f;
                float tentativeG = current.g + stepCost;

                if (!open.Contains(n) || tentativeG < n.g)
                {
                    n.parent = current;
                    n.g = tentativeG;
                    n.h = Heuristic(n, goal);
                    if (!open.Contains(n)) open.Add(n);
                }
            }
        }

        return null;
    }

    static float Heuristic(Node a, Node b)
    {
        // Diagonal-friendly heuristic (Chebyshev distance)
        float dx = Mathf.Abs(a.x - b.x);
        float dy = Mathf.Abs(a.y - b.y);
        float straight = Mathf.Abs(dx - dy);
        float diagonal = Mathf.Min(dx, dy);
        return diagonal * 1.4142f + straight;
    }

    static List<Node> Reconstruct(Node current)
    {
        var path = new List<Node>();
        while (current != null)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }
}
