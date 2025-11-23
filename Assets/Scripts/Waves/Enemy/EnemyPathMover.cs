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
        public float speed = 2f;

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
        
        
        private const float NODE_REACH_DISTANCE = 0.4f;

        Rigidbody2D rb;
        float timer;
        Vector2 currentDir;
        Vector3 startPos;

        private bool movementStopped = false;

        // ============================================================
        // Speed modifier
        // ============================================================
        private float speedModifier = 1f;
        public float FinalSpeed => speed * speedModifier;

        public void ApplySpeedModifier(float mul) => speedModifier = Mathf.Max(speedModifier*mul,0.6f);
        public void RemoveSpeedModifier(float mul) => speedModifier = 1;
        public void ClearSpeedModifiers() => speedModifier = 1f;
        // ============================================================

        // Cached world positions (HUGE improvement)
        private List<Vector2> cachedWorldPath = new List<Vector2>();


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
                // NOTE: If you want periodic repaths, call RecalculatePath() here
            }

            FollowPath();
        }

        public void ResetEnemy()
        {
            transform.position = startPos;
            rb.linearVelocity = Vector2.zero;
            index = 0;
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

            // Pre-cache all world positions (this is a HUGE optimization)
            cachedWorldPath.Clear();
            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    cachedWorldPath.Add(
                        GridManager.Instance.WorldFromGrid(path[i].x, path[i].y)
                    );
                }
            }
        }

        void FollowPath()
        {
            if (movementStopped || cachedWorldPath.Count == 0)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (index >= cachedWorldPath.Count)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 enemyPos = rb.position;
            Vector2 currentNode = cachedWorldPath[index];
            Vector2 dirToCurrent = (currentNode - enemyPos).normalized;

            Vector2 finalDir = dirToCurrent;

            float dist = Vector2.Distance(enemyPos, currentNode);

            // Smooth turning to next node
            if (dist < turnDistance && index + 1 < cachedWorldPath.Count)
            {
                Vector2 nextNode = cachedWorldPath[index + 1];
                Vector2 dirToNext = (nextNode - enemyPos).normalized;

                float t = 1f - (dist / turnDistance);
                finalDir = Vector2.Lerp(dirToCurrent, dirToNext, t);
            }

            // Smooth movement
            currentDir = Vector2.Lerp(currentDir, finalDir, Time.fixedDeltaTime * turnSmoothness);

            // Apply velocity
            rb.linearVelocity = currentDir * FinalSpeed;

            // Check if reached node
            if (dist < NODE_REACH_DISTANCE)
            {
                index++;
            }
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
#if UNITY_EDITOR
            if (!drawPath || cachedWorldPath == null || cachedWorldPath.Count == 0) return;

            Gizmos.color = pathColor;

            for (int i = 0; i < cachedWorldPath.Count - 1; i++)
            {
                Vector3 a = cachedWorldPath[i];
                Vector3 b = cachedWorldPath[i + 1];
                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.08f);
            }

            Vector3 goalPos = cachedWorldPath[cachedWorldPath.Count - 1];
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(goalPos, 0.12f);
            Gizmos.DrawWireSphere(goalPos, 0.18f);

            if (index < cachedWorldPath.Count)
            {
                Vector3 currentPos = cachedWorldPath[index];
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentPos, 0.1f);
            }
#endif
        }
    }
}
