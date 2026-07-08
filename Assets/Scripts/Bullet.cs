using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed    = 20f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int   damage   = 1;

    private Rigidbody2D rb;
    private int         _direction;
    private bool        _initialized;

    public void Init(float direction)
    {
        _direction   = (int)Mathf.Sign(direction);
        _initialized = true;

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = new Vector2(_direction * speed, 0f);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = direction < 0;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_initialized) return;

        // Ignorar al propio jugador
        if (other.GetComponentInParent<PlayerController>() != null) return;

        // Dañar enemigo
        EnemyHealth enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destruirse contra geometría sólida
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
