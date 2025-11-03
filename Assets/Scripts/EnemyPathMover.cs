using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPathMover : MonoBehaviour
{
    [Header("Target & Movement")]
    public Transform target;
    public float speed = 2f;
    public float repathInterval = 1.5f;
    [Range(0.1f, 2f)] public float turnDistance = 0.5f;   // when to start blending to next-next node
    [Range(1f, 20f)] public float turnSmoothness = 8f;    // how quickly to steer

    [Header("Debug")]
    public bool drawPath = true;
    public Color pathColor = Color.yellow;
    public bool resetRequested = false;

    [HideInInspector] public List<Node> path;
    [HideInInspector] public int index;

    Rigidbody2D rb;
    float timer;
    Vector2 currentDir;
    Vector3 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        RecalculatePath();
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;

        if (resetRequested)
        {
            ResetEnemy();
            resetRequested = false;
        }

        timer += Time.fixedDeltaTime;
        if (timer >= repathInterval)
        {
            timer = 0;
            //RecalculatePath();
        }

        FollowPath();
    }

    public void ResetEnemy()
    {
        transform.position = startPos;
        rb.linearVelocity = Vector2.zero;
        index = 0;
        RecalculatePath();
    }

    public void RecalculatePath()
    {
        if (!target || !GridManager.I) return;

        var (sx, sy) = GridManager.I.GridFromWorld(transform.position);
        var (tx, ty) = GridManager.I.GridFromWorld(target.position);

        var start = GridManager.I.GetNode(sx, sy);
        var goal = GridManager.I.GetNode(tx, ty);

        path = Pathfinder.FindPath(start, goal);
        index = 0;
    }

    void FollowPath()
    {
        if (path == null || index >= path.Count)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 currentNode = GridManager.I.WorldFromGrid(path[index].x, path[index].y);
        Vector2 dirToCurrent = (currentNode - rb.position).normalized;
        Vector2 finalDir = dirToCurrent;

        float dist = Vector2.Distance(rb.position, currentNode);

        // --- Blend toward the following node (smooth turn) ---
        if (dist < turnDistance && index + 1 < path.Count)
        {
            Vector2 nextNode = GridManager.I.WorldFromGrid(path[index + 1].x, path[index + 1].y);
            Vector2 dirToNext = (nextNode - rb.position).normalized;
            finalDir = Vector2.Lerp(dirToCurrent, dirToNext, 1f - (dist / turnDistance));
        }

        // --- Apply smooth steering ---
        currentDir = Vector2.Lerp(currentDir, finalDir, Time.fixedDeltaTime * turnSmoothness);
        rb.linearVelocity = currentDir * speed;

        // --- Advance waypoint when reached or passed ---
        if (HasReachedOrPassedNode(currentNode))
            index++;
    }
    
    bool HasReachedOrPassedNode(Vector2 nodePos)
    {
        // --- 1. proximity check ---
        if (Vector2.Distance(rb.position, nodePos) < 0.4f)
            return true;

        // --- 2. projected distance along segment ---
        if (index > 0)
        {
            Vector2 prev = GridManager.I.WorldFromGrid(path[index - 1].x, path[index - 1].y);

            Vector2 seg = nodePos - prev;
            Vector2 toEnemy = rb.position - prev;

            float segLenSq = seg.sqrMagnitude;
            float proj = Vector2.Dot(toEnemy, seg);

            // if projection is past segment length, we are beyond the node
            if (proj > segLenSq)
                return true;
        }

        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Did we hit a Nexus?
        if (other.TryGetComponent<Nexus>(out var nexus))
        {
            nexus.TakeDamage(10);   // 🔧 set your damage value
            Destroy(gameObject);    // enemy dies on impact
        }
    }

    // ============================================================
    // DEBUG GIZMOS
    void OnDrawGizmos()
    {
        if (!drawPath || path == null || path.Count == 0) return;

        // --- Path lines and waypoints ---
        Gizmos.color = pathColor;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 a = GridManager.I.WorldFromGrid(path[i].x, path[i].y);
            Vector3 b = GridManager.I.WorldFromGrid(path[i + 1].x, path[i + 1].y);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawSphere(a, 0.08f);
        }

        // --- Final target node highlight ---
        Vector3 goalPos = GridManager.I.WorldFromGrid(path[^1].x, path[^1].y);
        Gizmos.color = Color.cyan; // 👈 change this color as you wish
        Gizmos.DrawSphere(goalPos, 0.12f);
        Gizmos.DrawWireSphere(goalPos, 0.18f);

        // --- Optional: Draw current node the enemy is moving toward ---
        if (index < path.Count)
        {
            Vector3 currentPos = GridManager.I.WorldFromGrid(path[index].x, path[index].y);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentPos, 0.1f);
        }
    }

}
