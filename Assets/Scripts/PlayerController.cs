using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D  m_rigidbody2D;
    private Gatherinput  m_gatherinput;
    private Transform    m_transform;
    private Animator     m_animator;
    private Collider2D[] m_playerColliders;

    [SerializeField] private Animator   armsAnimator;
    [SerializeField] private GameObject bullet;
    [SerializeField] private Transform  firePoint;

    [Header("Lengua (TongueGrab)")]
    [Tooltip("Se autocompleta con GetComponent si lo dejas vacío.")]
    [SerializeField] private TongueGrab tongueGrab;

    [Header("Movement - Metal Slug")]
    [SerializeField] private float runSpeed                 = 7.5f;
    [SerializeField] private float jumpForce                = 14f;
    [SerializeField] private float fallGravityMultiplier    = 3f;
    [SerializeField] private float lowJumpGravityMultiplier = 2.4f;
    [SerializeField] private float coyoteTime               = 0.1f;
    [SerializeField] private float jumpBufferTime           = 0.1f;

    [Header("Correr (Sprint)")]
    [Tooltip("Tecla de teclado que hay que mantener presionada para correr.")]
    [SerializeField] private KeyCode runKey        = KeyCode.LeftShift;
    [Tooltip("Multiplicador de velocidad mientras corres (2 = el doble de rápido).")]
    [SerializeField] private float   runMultiplier = 1.6f;

    [Header("Deslizarse (Slide)")]
    [Tooltip("Tecla de teclado para deslizarse (como Duck Game).")]
    [SerializeField] private KeyCode slideKey       = KeyCode.LeftControl;
    [Tooltip("Velocidad inicial del impulso del slide.")]
    [SerializeField] private float   slideSpeed     = 14f;
    [Tooltip("Duración total del slide en segundos.")]
    [SerializeField] private float   slideDuration  = 0.35f;
    [Tooltip("Tiempo de espera antes de poder deslizarte de nuevo.")]
    [SerializeField] private float   slideCooldown  = 0.4f;
    [Tooltip("Collider normal, de pie (se desactiva durante el slide). Opcional.")]
    [SerializeField] private Collider2D standingCollider;
    [Tooltip("Collider bajo para el slide, p. ej. para pasar bajo obstáculos. Opcional.")]
    [SerializeField] private Collider2D slideCollider;

    [Header("Shooting")]
    [SerializeField] private float fireRate          = 0.12f;
    [SerializeField] private float bulletSpawnOffset = 0.25f;
    [SerializeField] private float shootRecoilForce  = 2f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce    = 8f;
    [SerializeField] private float knockbackUpForce  = 4f;
    [SerializeField] private float knockbackDuration = 0.25f;

    [SerializeField] private Transform lfoot;
    [SerializeField] private Transform rfoot;
    [SerializeField] private float     rayLength;
    [SerializeField] private LayerMask groundLayer;

    private int   idSpeed;
    private int   idIsGrounded;
    private int   idIsRunning;
    private int   idIsSliding;
    private int   direction = 1;

    private bool  isGrounded;
    private bool  isKnockedBack;
    private bool  isRunningHeld;
    private bool  isSliding;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private float nextFireTime;
    private float knockbackTimer;
    private float slideTimer;
    private float slideCooldownTimer;

    void Start()
    {
        m_rigidbody2D     = GetComponent<Rigidbody2D>();
        m_gatherinput     = GetComponent<Gatherinput>();
        m_transform       = GetComponent<Transform>();
        m_animator        = GetComponent<Animator>();
        m_playerColliders = GetComponentsInChildren<Collider2D>();

        if (tongueGrab == null) tongueGrab = GetComponent<TongueGrab>();

        if (lfoot == null) { var g = GameObject.Find("lfoot"); if (g) lfoot = g.transform; }
        if (rfoot == null) { var g = GameObject.Find("rfoot"); if (g) rfoot = g.transform; }

        idSpeed      = Animator.StringToHash("Speed");
        idIsGrounded = Animator.StringToHash("isGrounded");
        idIsRunning  = Animator.StringToHash("IsRunning");
        idIsSliding  = Animator.StringToHash("IsSliding");

        if (firePoint == null && armsAnimator != null)
            firePoint = armsAnimator.transform.Find("FirePoint");
    }

    private void Update()
    {
        if (m_gatherinput == null || m_animator == null) return;

        var gp = UnityEngine.InputSystem.Gamepad.current;

        // Correr: tecla en teclado o L1/Left Shoulder mantenido en el mando.
        bool runGamepad = gp != null && gp.leftShoulder.isPressed;
        isRunningHeld = Input.GetKey(runKey) || runGamepad;

        // Deslizarse: tecla en teclado o Y/Triángulo (North) en el mando.
        // (West/Cuadrado se deja libre porque tu Gatherinput ya lo usa para
        // disparar; South/East los usa TongueGrab para saltar-desde-el-gancho
        // y lanzar la lengua.)
        bool slideGamepad = gp != null && gp.buttonNorth.wasPressedThisFrame;
        if ((Input.GetKeyDown(slideKey) || slideGamepad) && CanSlide())
            StartSlide();

        SetAnimatorValues();
        HandleShootInput();
        UpdateJumpBuffer();
    }

    private void FixedUpdate()
    {
        if (m_gatherinput == null || m_rigidbody2D == null) return;

        CheckGround();

        // Mientras la lengua está enganchada y en péndulo, TongueGrab
        // es dueño absoluto del Rigidbody2D.
        if (tongueGrab != null && tongueGrab.IsAttached)
            return;

        if (slideCooldownTimer > 0f)
            slideCooldownTimer -= Time.fixedDeltaTime;

        if (isSliding)
        {
            UpdateSlide();
            return;
        }

        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f) isKnockedBack = false;
            ApplyJumpGravity();
            return;
        }

        HandleMovement();
        HandleJump();
        ApplyJumpGravity();
    }

    private void SetAnimatorValues()
    {
        m_animator.SetFloat(idSpeed, Mathf.Abs(m_gatherinput.ValueX));
        m_animator.SetBool(idIsGrounded, isGrounded);
        m_animator.SetBool(idIsRunning, isRunningHeld && Mathf.Abs(m_gatherinput.ValueX) > 0.1f);
        m_animator.SetBool(idIsSliding, isSliding);
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

        float speed = runSpeed * (isRunningHeld ? runMultiplier : 1f);
        m_rigidbody2D.linearVelocity = new Vector2(
            speed * m_gatherinput.ValueX,
            m_rigidbody2D.linearVelocity.y);
    }

    private void Flip()
    {
        if (m_gatherinput.ValueX * direction < 0)
        {
            m_transform.localScale = new Vector3(-m_transform.localScale.x, 1f, 1f);
            direction *= -1;
        }
    }

    private void HandleJump()
    {
        if (isGrounded) coyoteCounter = coyoteTime;
        else            coyoteCounter -= Time.fixedDeltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            m_rigidbody2D.linearVelocity = new Vector2(runSpeed * m_gatherinput.ValueX, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter     = 0f;
            isGrounded        = false;
        }

        m_gatherinput.IsJumping = false;
    }

    private void ApplyJumpGravity()
    {
        if (isGrounded) return;

        float gravity = Physics2D.gravity.y * m_rigidbody2D.gravityScale;

        if (m_rigidbody2D.linearVelocity.y < 0f)
            m_rigidbody2D.linearVelocity += Vector2.up * gravity * (fallGravityMultiplier - 1f)    * Time.fixedDeltaTime;
        else if (m_rigidbody2D.linearVelocity.y > 0f && !m_gatherinput.IsJumpHeld)
            m_rigidbody2D.linearVelocity += Vector2.up * gravity * (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
    }

    private void CheckGround()
    {
        if (lfoot == null || rfoot == null) { isGrounded = false; return; }

        isGrounded =
            Physics2D.Raycast(lfoot.position, Vector2.down, rayLength, groundLayer) ||
            Physics2D.Raycast(rfoot.position, Vector2.down, rayLength, groundLayer);
    }

    private void HandleShootInput()
    {
        if (!m_gatherinput.IsShooting) return;

        m_gatherinput.IsShooting = false;

        if (Time.time < nextFireTime) return;

        Shoot();
        nextFireTime = Time.time + fireRate;
    }

    private void Shoot()
    {
        if (armsAnimator != null)
            armsAnimator.SetTrigger("Shoot");

        ApplyShootRecoil();

        if (bullet == null || firePoint == null) return;

        // Spawn adelantado para no nacer dentro del enemigo
        Vector3 spawnPos = firePoint.position + Vector3.right * direction * bulletSpawnOffset;
        GameObject bulletInstance = Instantiate(bullet, spawnPos, Quaternion.identity);

        // Ignorar colisiones con el propio jugador inmediatamente
        Collider2D bulletCollider = bulletInstance.GetComponent<Collider2D>();
        if (bulletCollider != null)
            foreach (Collider2D pc in m_playerColliders)
                if (pc != null) Physics2D.IgnoreCollision(bulletCollider, pc);

        // Inicializar dirección — el bullet ya puede colisionar con enemigos desde frame 0
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.Init(direction);
    }

    private void ApplyShootRecoil()
    {
        if (m_rigidbody2D == null) return;
        m_rigidbody2D.linearVelocity = new Vector2(
            m_rigidbody2D.linearVelocity.x - direction * shootRecoilForce,
            m_rigidbody2D.linearVelocity.y);
    }

    // ── Deslizarse (Slide, estilo Duck Game) ──────────────────────────

    private bool CanSlide()
    {
        return isGrounded
            && !isSliding
            && !isKnockedBack
            && slideCooldownTimer <= 0f
            && (tongueGrab == null || !tongueGrab.IsAttached);
    }

    private void StartSlide()
    {
        isSliding          = true;
        slideTimer         = slideDuration;
        slideCooldownTimer = slideCooldown;

        m_rigidbody2D.linearVelocity = new Vector2(
            direction * slideSpeed,
            m_rigidbody2D.linearVelocity.y);

        if (standingCollider != null) standingCollider.enabled = false;
        if (slideCollider    != null) slideCollider.enabled    = true;
    }

    private void UpdateSlide()
    {
        slideTimer -= Time.fixedDeltaTime;

        // Deceleración constante manteniendo la dirección del slide,
        // así el impulso se siente fuerte al inicio y se apaga solo.
        float decel = slideSpeed / slideDuration;
        float newX  = Mathf.MoveTowards(m_rigidbody2D.linearVelocity.x, 0f, decel * Time.fixedDeltaTime);
        m_rigidbody2D.linearVelocity = new Vector2(newX, m_rigidbody2D.linearVelocity.y);

        ApplyJumpGravity();

        // Cancelar el slide saltando (útil para encadenar slide → salto).
        bool jumpPressed = jumpBufferCounter > 0f && isGrounded;

        if (slideTimer <= 0f || jumpPressed)
            EndSlide();
    }

    private void EndSlide()
    {
        isSliding = false;

        if (standingCollider != null) standingCollider.enabled = true;
        if (slideCollider    != null) slideCollider.enabled    = false;
    }

    public void ApplyKnockback(Vector2 enemyPosition)
    {
        isKnockedBack  = true;
        knockbackTimer = knockbackDuration;

        Vector2 dir = ((Vector2)transform.position - enemyPosition).normalized;
        m_rigidbody2D.linearVelocity = Vector2.zero;
        m_rigidbody2D.AddForce(
            new Vector2(dir.x * knockbackForce, knockbackUpForce),
            ForceMode2D.Impulse);
    }

    // ── Heal (llamado por TongueGrab al tragar un enemigo) ────────────
    public void Heal(float amount)
    {
        // Conecta aquí con tu sistema de vida actual.
        // Ejemplo si tienes un PlayerHealth component:
        // GetComponent<PlayerHealth>()?.Heal(amount);
    }
}
