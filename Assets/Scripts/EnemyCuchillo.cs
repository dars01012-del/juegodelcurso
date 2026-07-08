using UnityEngine;

public class EnemyCuchillo : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Ataque")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Detección")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundDistance = 0.5f;
    [SerializeField] private float wallDistance = 0.2f;

    private Rigidbody2D rb;
    private PlayerHealth playerHealth;

    private int direction = 1;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("No se encontró PlayerHealth");
        }
    }

    private void FixedUpdate()
    {
        if (playerHealth != null)
        {
            float distance = Vector2.Distance(
                transform.position,
                playerHealth.transform.position
            );

            if (distance <= attackRange)
            {
                Attack();
                return;
            }

            if (distance <= detectionRange)
            {
                ChasePlayer();
                return;
            }
        }

        Patrol();
    }

    private void Patrol()
    {
        rb.linearVelocity = new Vector2(
            direction * moveSpeed,
            rb.linearVelocity.y
        );

        if (groundCheck == null || wallCheck == null)
            return;

        bool groundDetected = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            groundDistance,
            groundLayer
        );

        bool wallDetected = Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * direction,
            wallDistance,
            groundLayer
        );

        if (!groundDetected || wallDetected)
        {
            Flip();
        }
    }

    private void ChasePlayer()
    {
        float distanceX =
            playerHealth.transform.position.x - transform.position.x;

        // Evita que se vuelva loco cuando está encima del jugador
        if (Mathf.Abs(distanceX) < 0.1f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float playerDirection = Mathf.Sign(distanceX);

        // IMPORTANTE
        direction = (int)playerDirection;

        rb.linearVelocity = new Vector2(
            playerDirection * moveSpeed,
            rb.linearVelocity.y
        );

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * playerDirection;
        transform.localScale = scale;
    }

    private void Flip()
    {
        direction *= -1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    private void Attack()
    {
        rb.linearVelocity = Vector2.zero;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (playerHealth != null)
        {
           playerHealth.TakeDamage(
    attackDamage,
    transform.position
);
            Debug.Log("Daño aplicado al jugador");
        }
    }
}