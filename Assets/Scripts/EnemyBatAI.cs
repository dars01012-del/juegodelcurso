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
    public Transform player;
    public Rigidbody2D rb;
    public Animator animator;

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float chaseRange = 6f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    public int health = 3;

    [Header("Flight")]
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 2f;

    private State currentState;
    private float attackTimer;
    private Vector2 startPos;
    private bool isAttacking;

    void Start()
    {
        startPos = transform.position;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");

            if (p != null)
                player = p.transform;
        }

        ChangeState(State.Sleep);
    }

    void Update()
    {
        if (player == null) return;
        if (currentState == State.Die) return;

        attackTimer -= Time.deltaTime;

        float distance =
            Vector2.Distance(transform.position, player.position);

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

                if (distance <= attackRange &&
                    attackTimer <= 0 &&
                    !isAttacking)
                {
                    ChangeState(State.Attack);
                }

                break;

            case State.Attack:
                Attack();
                break;

            case State.Hurt:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    void IdleMove()
    {
        float y =
            Mathf.Sin(Time.time * floatFrequency)
            * floatAmplitude;

        transform.position =
            new Vector2(
                transform.position.x,
                startPos.y + y
            );
    }

    void ChasePlayer()
    {
        Vector2 dir =
            (player.position - transform.position).normalized;

        rb.linearVelocity = dir * moveSpeed;

        if (dir.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (dir.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void Attack()
    {
        if (isAttacking)
            return;

        isAttacking = true;

        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger("Attack");
    }

    // Animation Event al final de la animación Attack
    public void EndAttack()
    {
        isAttacking = false;

        attackTimer = attackCooldown;

        ChangeState(State.Chase);
    }

    public void TakeDamage(int damage)
    {
        if (currentState == State.Die)
            return;

        health -= damage;

        animator.SetTrigger("Hurt");

        if (health <= 0)
        {
            ChangeState(State.Die);
        }
        else
        {
            ChangeState(State.Hurt);
            Invoke(nameof(BackToChase), 0.3f);
        }
    }

    void BackToChase()
    {
        if (currentState != State.Die)
            ChangeState(State.Chase);
    }

    void Die()
    {
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger("Die");

        Destroy(gameObject, 1.5f);
    }

    void ChangeState(State newState)
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
                Invoke(nameof(GoIdle), 0.5f);
                break;

            case State.Idle:
                animator.SetBool("Sleep", false);
                break;

            case State.Chase:
                animator.SetBool("Sleep", false);
                break;

            case State.Die:
                Die();
                break;
        }
    }

    void GoIdle()
    {
        ChangeState(State.Idle);
    }
}