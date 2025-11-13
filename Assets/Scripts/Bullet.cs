using Enemy;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float speed;
    private int damage;
    private float maxRange;

    private Vector2 startPos;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float speed, int damage, float maxRange)
    {
        this.speed = speed;
        this.damage = damage;
        this.maxRange = maxRange;

        startPos = transform.position;

        if (rb)
            rb.linearVelocity = direction.normalized * speed;
    }

    void Update()
    {
        // Destroy when exceeding range
        if (Vector2.Distance(startPos, transform.position) >= maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<EnemyHealth>(out var enemy))
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}