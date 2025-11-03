using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float range = 10f;
    [SerializeField] int damage = 1;

    Vector2 startPos;
    Vector2 direction;

    void Start()
    {
        startPos = transform.position;
    }

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));

        // destroy after traveling past range
        if (Vector2.Distance(startPos, transform.position) >= range)
            Destroy(gameObject);
    }

    // Later we’ll use this for hitting enemies
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<EnemyHealth>(out var enemy))
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject); // bullet disappears on hit
        }
    }
}

