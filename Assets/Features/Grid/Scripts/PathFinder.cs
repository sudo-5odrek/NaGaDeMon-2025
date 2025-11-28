using System.Collections.Generic;
using UnityEngine;

namespace NaGaDeMon.Features.Grid
{
    public static class Pathfinder
    {
        // cardinal + diagonal offsets
        static readonly Vector2Int[] cardinalDirs = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };
        static readonly Vector2Int[] diagonalDirs = {
            new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
        };

        public static bool allowCornerCutting = false;

        // 🔹 Now takes a “bool allowDiagonals”
        public static List<Node> FindPath(Node start, Node goal, bool allowDiagonals = true)
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

                // 🔹 Use only the directions we allow
                var dirs = allowDiagonals ? GetAllDirs() : cardinalDirs;

                foreach (var dir in dirs)
                {
                    var n = GridManager.Instance.GetNode(current.x + dir.x, current.y + dir.y);
                    if (n == null || !n.walkable || closed.Contains(n)) continue;

                    // prevent diagonal corner cutting
                    if (allowDiagonals && Mathf.Abs(dir.x) + Mathf.Abs(dir.y) == 2 && !allowCornerCutting)
                    {
                        Node sideA = GridManager.Instance.GetNode(current.x + dir.x, current.y);
                        Node sideB = GridManager.Instance.GetNode(current.x, current.y + dir.y);
                        if ((sideA != null && !sideA.walkable) || (sideB != null && !sideB.walkable))
                            continue;
                    }

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

        static Vector2Int[] GetAllDirs()
        {
            Vector2Int[] dirs = new Vector2Int[cardinalDirs.Length + diagonalDirs.Length];
            cardinalDirs.CopyTo(dirs, 0);
            diagonalDirs.CopyTo(dirs, cardinalDirs.Length);
            return dirs;
        }

        static float Heuristic(Node a, Node b)
        {
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
}
