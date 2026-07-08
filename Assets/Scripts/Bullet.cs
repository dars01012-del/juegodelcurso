using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 1;

    private Rigidbody2D rb;

    public void Init(float direction)
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        rb.linearVelocity = new Vector2(direction * speed, 0f);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignorar al jugador
        if (other.GetComponentInParent<PlayerController>() != null)
            return;

        // Buscar cualquier enemigo que tenga EnemyHealth
        EnemyHealth enemyHealth =
            other.GetComponentInParent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Destruir la bala al chocar con paredes o suelo
        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}