using UnityEngine;

public class EnemyBatAI : MonoBehaviour
{
    public enum State
    {
        Sleep,
        WakeUp,
        Idle,
        Chase,
        Attack,
        Hurt,
        Die
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float chaseRange = 6f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 1;

    [Header("Flight")]
    [SerializeField] private float floatAmplitude = 0.35f;
    [SerializeField] private float floatFrequency = 2f;

    [Header("Timing")]
    [SerializeField] private float attackDuration = 0.55f;
    [SerializeField] private float hurtDuration = 0.3f;

    private State currentState;
    private float attackTimer;
    private Vector2 startPos;
    private bool isAttacking;
    private bool isDead;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth != null)
            enemyHealth.OnDamaged += HandleDamaged;
    }

    private void Start()
    {
        startPos = transform.position;

        if (player == null)
        {
            PlayerHealth foundPlayer = FindFirstObjectByType<PlayerHealth>();
            if (foundPlayer != null)
                player = foundPlayer.transform;
        }

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        ChangeState(State.Sleep);
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
            enemyHealth.OnDamaged -= HandleDamaged;
    }

    private void Update()
    {
        if (player == null || isDead)
            return;

        attackTimer -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Sleep:
                if (distance <= chaseRange)
                    ChangeState(State.WakeUp);
                break;

            case State.WakeUp:
                break;

            case State.Idle:
                IdleMove();
                if (distance <= chaseRange)
                    ChangeState(State.Chase);
                break;

            case State.Chase:
                ChasePlayer();
                if (distance <= attackRange && attackTimer <= 0f && !isAttacking)
                    ChangeState(State.Attack);
                break;

            case State.Attack:
                Attack();
                break;

            case State.Hurt:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void HandleDamaged(int damage)
    {
        if (isDead)
            return;

        if (enemyHealth != null && enemyHealth.CurrentHealth <= 0)
            return;

        animator.SetTrigger("Hurt");
        ChangeState(State.Hurt);
        CancelInvoke(nameof(ResumeFromHurt));
        Invoke(nameof(ResumeFromHurt), hurtDuration);
    }

    private void ResumeFromHurt()
    {
        if (isDead || currentState == State.Die)
            return;

        ChangeState(State.Chase);
    }

    public void HandleDeath()
    {
        if (isDead)
            return;

        isDead = true;
        isAttacking = false;
        CancelInvoke(nameof(DealAttackDamage));
        CancelInvoke(nameof(EndAttack));
        CancelInvoke(nameof(ResumeFromHurt));
        CancelInvoke(nameof(GoIdle));

        rb.linearVelocity = Vector2.zero;
        currentState = State.Die;
        animator.SetTrigger("Die");

        Destroy(gameObject, 1.5f);
    }

    private void IdleMove()
    {
        float y = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector2(transform.position.x, startPos.y + y);
    }

    private void ChasePlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        if (dir.x > 0f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (dir.x < 0f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    private void Attack()
    {
        if (isAttacking)
            return;

        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("Attack");

        Invoke(nameof(DealAttackDamage), attackDuration * 0.45f);
        Invoke(nameof(EndAttack), attackDuration);
    }

    private void DealAttackDamage()
    {
        if (player == null || playerHealth == null || currentState != State.Attack)
            return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= attackRange * 1.25f)
            playerHealth.TakeDamage(attackDamage, transform.position);
    }

    public void EndAttack()
    {
        if (isDead)
            return;

        isAttacking = false;
        attackTimer = attackCooldown;
        ChangeState(State.Chase);
    }

    private void ChangeState(State newState)
    {
        currentState = newState;

        switch (newState)
        {
            case State.Sleep:
                animator.SetBool("Sleep", true);
                break;

            case State.WakeUp:
                animator.SetBool("Sleep", false);
                animator.SetTrigger("WakeUp");
                CancelInvoke(nameof(GoIdle));
                Invoke(nameof(GoIdle), 0.5f);
                break;

            case State.Idle:
            case State.Chase:
                animator.SetBool("Sleep", false);
                break;
        }
    }

    private void GoIdle()
    {
        if (!isDead)
            ChangeState(State.Idle);
    }
}
