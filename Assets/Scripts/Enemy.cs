using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRange = 6f;

    [Header("Ataque")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 1;

    private Transform player;
    private Rigidbody2D rb;
    private PlayerHealth playerHealth;

    private float nextAttackTime;
    private bool isAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        PlayerHealth foundPlayer = FindFirstObjectByType<PlayerHealth>();
        if (foundPlayer != null)
        {
            player = foundPlayer.transform;
            playerHealth = foundPlayer;
        }
    }

    private void FixedUpdate()
    {
        if (player == null || isAttacking)
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
            Attack();
        else if (distance <= detectionRange)
            MoveToPlayer();
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void MoveToPlayer()
    {
        float direction = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

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
        isAttacking = true;

        if (playerHealth != null)
            playerHealth.TakeDamage(attackDamage, transform.position);

        Invoke(nameof(FinishAttack), 0.4f);
    }

    private void FinishAttack()
    {
        isAttacking = false;
    }
}
