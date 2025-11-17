// (everything else stays identical to your original file)
using System.Collections.Generic;
using Grid;
using UnityEngine;

namespace Enemy
{
    [ExecuteAlways]
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyPathMover : MonoBehaviour
    {
        [Header("Target & Movement")]
        public Transform target;
        public float speed = 2f; // ← we keep this exactly as-is

        public float repathInterval = 1.5f;
        [Range(0.1f, 2f)] public float turnDistance = 0.5f;
        [Range(1f, 20f)] public float turnSmoothness = 8f;

        [Header("Debug")]
        public bool drawPath = true;
        public Color pathColor = Color.yellow;
        public bool resetRequested = false;

        public EnemyAttack myAttackPattern;

        [HideInInspector] public List<Node> path;
        [HideInInspector] public int index;

        Rigidbody2D rb;
        float timer;
        Vector2 currentDir;
        Vector3 startPos;

        private bool movementStopped = false;

        // ============================================================
        // NEW — speed modifier
        // ============================================================
        private float speedModifier = 1f;

        public float FinalSpeed => speed * speedModifier; // NEW

        public void ApplySpeedModifier(float mul) // NEW
        {
            speedModifier *= mul;
        }

        public void RemoveSpeedModifier(float mul) // NEW
        {
            speedModifier /= mul;
        }

        public void ClearSpeedModifiers() // optional NEW
        {
            speedModifier = 1f;
        }
        // ============================================================


        public void StopMovement()
        {
            movementStopped = true;
            rb.linearVelocity = Vector2.zero;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            startPos = transform.position;
        }

        private void OnEnable()
        {
            GridManager.Instance.OnGridUpdated += HandleGridUpdate;
        }

        private void OnDisable()
        {
            if (GridManager.Instance != null)
                GridManager.Instance.OnGridUpdated -= HandleGridUpdate;
        }

        private void HandleGridUpdate()
        {
            RecalculatePath();
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
            }

            FollowPath();
        }

        public void ResetEnemy()
        {
            transform.position = startPos;
            rb.linearVelocity = Vector2.zero;
            index = 0;

            // NEW — resets slow/hast modifiers
            ClearSpeedModifiers();

            movementStopped = false;
            RecalculatePath();
        }

        public void RecalculatePath()
        {
            if (!target || !GridManager.Instance) return;

            var (sx, sy) = GridManager.Instance.GridFromWorld(transform.position);
            var (tx, ty) = GridManager.Instance.GridFromWorld(target.position);

            var start = GridManager.Instance.GetNode(sx, sy);
            var goal = GridManager.Instance.GetNode(tx, ty);

            path = Pathfinder.FindPath(start, goal, true);
            index = 0;
        }

        void FollowPath()
        {
            if (movementStopped)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (path == null || index >= path.Count)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 currentNode = GridManager.Instance.WorldFromGrid(path[index].x, path[index].y);
            Vector2 dirToCurrent = (currentNode - rb.position).normalized;
            Vector2 finalDir = dirToCurrent;

            float dist = Vector2.Distance(rb.position, currentNode);

            if (dist < turnDistance && index + 1 < path.Count)
            {
                Vector2 nextNode = GridManager.Instance.WorldFromGrid(path[index + 1].x, path[index + 1].y);
                Vector2 dirToNext = (nextNode - rb.position).normalized;
                finalDir = Vector2.Lerp(dirToCurrent, dirToNext, 1f - (dist / turnDistance));
            }

            currentDir = Vector2.Lerp(currentDir, finalDir, Time.fixedDeltaTime * turnSmoothness);

            // ============================================================
            // NEW — use final modified speed instead of plain speed
            // ============================================================
            rb.linearVelocity = currentDir * FinalSpeed;

            if (HasReachedOrPassedNode(currentNode))
                index++;
        }

        bool HasReachedOrPassedNode(Vector2 nodePos)
        {
            if (Vector2.Distance(rb.position, nodePos) < 1f)
                return true;

            if (index > 0)
            {
                Vector2 prev = GridManager.Instance.WorldFromGrid(path[index - 1].x, path[index - 1].y);

                Vector2 seg = nodePos - prev;
                Vector2 toEnemy = rb.position - prev;

                float segLenSq = seg.sqrMagnitude;
                float proj = Vector2.Dot(toEnemy, seg);

                if (proj > segLenSq)
                    return true;
            }

            return false;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Nexus>(out var nexus))
            {
                myAttackPattern.OnReachNexus(nexus);
                rb.linearVelocity = Vector3.zero;
            }
        }

        void OnDrawGizmos()
        {
            if (!drawPath || path == null || path.Count == 0) return;

            Gizmos.color = pathColor;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 a = GridManager.Instance.WorldFromGrid(path[i].x, path[i].y);
                Vector3 b = GridManager.Instance.WorldFromGrid(path[i + 1].x, path[i + 1].y);
                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.08f);
            }

            Vector3 goalPos = GridManager.Instance.WorldFromGrid(path[^1].x, path[^1].y);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(goalPos, 0.12f);
            Gizmos.DrawWireSphere(goalPos, 0.18f);

            if (index < path.Count)
            {
                Vector3 currentPos = GridManager.Instance.WorldFromGrid(path[index].x, path[index].y);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentPos, 0.1f);
            }
        }
    }
}
