using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D m_rigidbody2D;
    private Gatherinput m_gatherinput;
    private Transform m_transform;
    private Animator m_animator;
    private Collider2D[] m_playerColliders;

    [SerializeField] private Animator armsAnimator;
    [SerializeField] private GameObject bullet;
    [SerializeField] private Transform firePoint;

    [Header("Movement - Metal Slug")]
    [SerializeField] private float runSpeed = 6.5f;
    [SerializeField] private float jumpForce = 13f;
    [SerializeField] private float fallGravityMultiplier = 2.8f;
    [SerializeField] private float lowJumpGravityMultiplier = 2.2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Shooting")]
    [SerializeField] private float fireRate = 0.18f;
    [SerializeField] private float bulletSpawnOffset = 0.12f;

    private int idSpeed;
    private int idIsGrounded;
    private int direction = 1;
    private bool isKnockedBack;


    [SerializeField] private Transform lfoot, rfoot;
    [SerializeField] private float rayLength;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool isGrounded;
    private float knockbackTimer;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float nextFireTime;
    [Header("Knockback")]
[SerializeField] private float knockbackForce = 8f;
[SerializeField] private float knockbackUpForce = 4f;
[SerializeField] private float knockbackDuration = 0.25f;

    void Start()
    {
        m_rigidbody2D = GetComponent<Rigidbody2D>();
        m_gatherinput = GetComponent<Gatherinput>();
        m_transform = GetComponent<Transform>();
        m_animator = GetComponent<Animator>();
        m_playerColliders = GetComponentsInChildren<Collider2D>();

        GameObject lfootObject = GameObject.Find("lfoot");
        GameObject rfootObject = GameObject.Find("rfoot");

        if (lfootObject != null)
            lfoot = lfootObject.transform;

        if (rfootObject != null)
            rfoot = rfootObject.transform;

        idSpeed = Animator.StringToHash("Speed");
        idIsGrounded = Animator.StringToHash("isGrounded");

        if (firePoint == null && armsAnimator != null)
            firePoint = armsAnimator.transform.Find("FirePoint");
    }

    private void Update()
    {
        if (m_gatherinput == null || m_animator == null)
            return;

        SetAnimatorValues();
        HandleShootInput();
        UpdateJumpBuffer();
    }
void FixedUpdate()
{
    if (m_gatherinput == null || m_rigidbody2D == null)
        return;

    CheckGround();

    if (isKnockedBack)
    {
        knockbackTimer -= Time.fixedDeltaTime;

        if (knockbackTimer <= 0f)
            isKnockedBack = false;

        ApplyJumpGravity();
        return;
    }

    HandleMovement();
    HandleJump();
    ApplyJumpGravity();
}
    private void SetAnimatorValues()
    {
        float moveInput = Mathf.Abs(m_gatherinput.ValueX);
        m_animator.SetFloat(idSpeed, moveInput);
        m_animator.SetBool(idIsGrounded, isGrounded);
    }

    private void UpdateJumpBuffer()
    {
        if (m_gatherinput.IsJumping)
            jumpBufferCounter = jumpBufferTime;

        jumpBufferCounter -= Time.deltaTime;
    }

    private void HandleMovement()
    {
        Flip();

        float inputX = m_gatherinput.ValueX;
        float targetSpeed = runSpeed * inputX;

        m_rigidbody2D.linearVelocity = new Vector2(targetSpeed, m_rigidbody2D.linearVelocity.y);
    }

    private void Flip()
    {
        if (m_gatherinput.ValueX * direction < 0)
        {
            m_transform.localScale = new Vector3(-m_transform.localScale.x, 1, 1);
            direction *= -1;
        }
    }

    private void HandleJump()
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            m_rigidbody2D.linearVelocity = new Vector2(
                runSpeed * m_gatherinput.ValueX,
                jumpForce
            );

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            isGrounded = false;
        }

        m_gatherinput.IsJumping = false;
    }

    private void ApplyJumpGravity()
    {
        if (isGrounded)
            return;

        float gravityScale = m_rigidbody2D.gravityScale;
        float gravity = Physics2D.gravity.y * gravityScale;

        if (m_rigidbody2D.linearVelocity.y < 0f)
        {
            m_rigidbody2D.linearVelocity += Vector2.up * gravity * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (m_rigidbody2D.linearVelocity.y > 0f && !m_gatherinput.IsJumpHeld)
        {
            m_rigidbody2D.linearVelocity += Vector2.up * gravity * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void CheckGround()
    {
        if (lfoot == null || rfoot == null)
        {
            isGrounded = false;
            return;
        }

        RaycastHit2D lfootRay = Physics2D.Raycast(lfoot.position, Vector2.down, rayLength, groundLayer);
        RaycastHit2D rfootRay = Physics2D.Raycast(rfoot.position, Vector2.down, rayLength, groundLayer);

        isGrounded = lfootRay || rfootRay;
    }

    private void HandleShootInput()
    {
        if (!m_gatherinput.IsShooting)
            return;

        m_gatherinput.IsShooting = false;

        if (Time.time < nextFireTime)
            return;

        Shoot();
        nextFireTime = Time.time + fireRate;
    }

    private void Shoot()
    {
        if (armsAnimator != null)
            armsAnimator.SetTrigger("Shoot");

        if (bullet == null || firePoint == null)
            return;

        Vector2 spawnDirection = Vector2.right * direction;
        Vector3 spawnPosition = firePoint.position + (Vector3)(spawnDirection * bulletSpawnOffset);

        GameObject bulletInstance = Instantiate(bullet, spawnPosition, Quaternion.identity);
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();

        if (bulletScript != null)
            bulletScript.Init(direction);

        Collider2D bulletCollider = bulletInstance.GetComponent<Collider2D>();
        if (bulletCollider == null)
            return;

        foreach (Collider2D playerCollider in m_playerColliders)
        {
            if (playerCollider != null)
                Physics2D.IgnoreCollision(bulletCollider, playerCollider);
        }
    }public void ApplyKnockback(Vector2 enemyPosition)
{
    isKnockedBack = true;
    knockbackTimer = knockbackDuration;

    Vector2 knockbackDirection =
        ((Vector2)transform.position - enemyPosition).normalized;

    m_rigidbody2D.linearVelocity = Vector2.zero;

    m_rigidbody2D.AddForce(
        new Vector2(
            knockbackDirection.x * knockbackForce,
            knockbackUpForce
        ),
        ForceMode2D.Impulse
    );
}
}
