using System.Collections;
using UnityEngine;

/// <summary>
/// TongueGrab — Lengua tipo gancho con física de péndulo + salto desde el enganche.
///
/// Fases:
///   Idle → Extending → [HitEnemy] daño + impulso leve, vuelve a Idle
///                    → [HitSurface] Swinging (péndulo real)
///                                  → soltar botón / Jump = lanzamiento
///                                  → tocar el suelo = suelta automático
///                                  → timer agotado = suelta solo
/// Anti-spam: cooldown + un solo uso en el aire (recarga al aterrizar).
///
/// NOTA PARA EL SCRIPT DE MOVIMIENTO:
/// Mientras `IsAttached` sea true, tu PlayerController debería IGNORAR
/// el input horizontal normal (o reducirlo mucho), porque si ambos scripts
/// aplican fuerzas al mismo Rigidbody2D al mismo tiempo, el swing se siente
/// "trabado" o errático. Ejemplo típico en tu PlayerController:
///
///     if (!tongueGrab.IsAttached) { /* tu movimiento normal aquí */ }
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TongueGrab : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────

    [Header("Referencias")]
    [SerializeField] private Transform        tongueOrigin;
    [SerializeField] private PlayerController playerController;

    [Header("Lengua")]
    [SerializeField] private float tongueSpeed        = 32f;
    [SerializeField] private float maxTongueLength    = 6f;
    [SerializeField] private float tongueRetractSpeed = 28f;

    [Header("Lengua Procedural")]
    [Tooltip("Cuántos segmentos tiene la lengua (más = más suave, más CPU).")]
    [SerializeField] [Range(4, 24)] private int   tongueSegments   = 10;
    [Tooltip("Amplitud máxima de la ondulación lateral.")]
    [SerializeField] private float waveAmplitude  = 0.18f;
    [Tooltip("Frecuencia de la onda (ciclos por unidad de longitud).")]
    [SerializeField] private float waveFrequency  = 3.5f;
    [Tooltip("Velocidad a la que viaja la onda a lo largo de la lengua.")]
    [SerializeField] private float waveSpeed      = 6f;
    [Tooltip("Cuánto reduce la onda cerca de la base y la punta (0=nada, 1=total).")]
    [SerializeField] [Range(0f,1f)] private float waveTaper   = 0.85f;
    [Tooltip("La onda se amplifica con la velocidad del jugador.")]
    [SerializeField] private float waveVelocityInfluence = 0.04f;

    [Header("Apariencia de lengua")]
    [Tooltip("Color de la base de la lengua (rosa/carne por defecto).")]
    [SerializeField] private Color tongueColorBase = new Color(1f, 0.42f, 0.55f);
    [Tooltip("Color en la punta de la lengua.")]
    [SerializeField] private Color tongueColorTip  = new Color(1f, 0.62f, 0.7f);
    [Tooltip("Grosor en la base (donde sale de la boca).")]
    [SerializeField] private float tongueWidthBase = 0.16f;
    [Tooltip("Grosor en la punta.")]
    [SerializeField] private float tongueWidthTip  = 0.06f;
    [Tooltip("Capa de orden de dibujo (para que la lengua no quede tapada por el sprite).")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int    sortingOrder     = 10;

    [Header("Lengua en reposo (Idle)")]
    [Tooltip("Si está activo, la lengua queda siempre visible asomando un poco de la boca aunque no la estés usando.")]
    [SerializeField] private bool  showIdleTongue   = true;
    [Tooltip("Largo del 'pico' de lengua en reposo.")]
    [SerializeField] private float idleTongueLength = 0.22f;
    [Tooltip("Velocidad del balanceo suave de la lengua en reposo.")]
    [SerializeField] private float idleWiggleSpeed  = 2.5f;

    [Header("Capas")]
    [SerializeField] private LayerMask grabLayer;
    [SerializeField] private LayerMask surfaceLayer;

    [Header("Péndulo")]
    [Tooltip("Amortiguación del péndulo (0 = sin freno, 0.015 = ligero, 0.04 = pesado).")]
    [SerializeField] [Range(0f, 0.06f)] private float swingDamping   = 0.032f;
    [Tooltip("Fuerza que el input horizontal agrega al columpio.")]
    [SerializeField] private float swingInputForce  = 9f;
    [Tooltip("Segundos máximos colgado antes de soltarse automático.")]
    [SerializeField] private float maxSwingTime     = 1.8f;

    [Header("Salto desde el gancho")]
    [Tooltip("Multiplicador sobre la velocidad tangencial al soltar (feeling de lanzamiento).")]
    [SerializeField] private float releaseBoost     = 1.25f;
    [Tooltip("Velocidad mínima garantizada al soltar, para que nunca caiga en seco.")]
    [SerializeField] private float minReleaseSpeed  = 5f;
    [Tooltip("Fuerza del salto si presionas Jump mientras estás colgado.")]
    [SerializeField] private float jumpFromGrapple  = 16f;

    [Header("Golpe a enemigo")]
    [SerializeField] private int   tongueDamage     = 1;
    [SerializeField] private float enemyLaunchForce = 9f;
    [Tooltip("Vida que recupera el jugador al 'tragar' un enemigo con la lengua. 0 = desactivado.")]
    [SerializeField] private float healPerEnemyHit  = 5f;

    [Header("Anti-spam")]
    [SerializeField] private float cooldown          = 0.55f;
    [SerializeField] private bool  rechargeOnGround  = true;

    [Header("Detección de suelo")]
    [Tooltip("Offset hacia abajo desde el pivote del personaje para el chequeo de suelo.")]
    [SerializeField] private float groundCheckOffset = 0.65f;
    [Tooltip("Radio del chequeo de suelo.")]
    [SerializeField] private float groundCheckRadius  = 0.18f;

    [Header("Sonidos")]
    [SerializeField] private AudioClip sfxExtend;
    [SerializeField] private AudioClip sfxHitEnemy;
    [SerializeField] private AudioClip sfxHitSurface;
    [SerializeField] private AudioClip sfxReady;

    // ── Estado ───────────────────────────────────────────────────────

    public TongueState State { get; private set; } = TongueState.Idle;

    /// <summary>True mientras la lengua está enganchada y en péndulo (útil para que
    /// el script de movimiento del personaje ceda el control del input horizontal).</summary>
    public bool IsAttached => State == TongueState.Swinging;

    private LineRenderer _line;
    private AudioSource  _audio;
    private Rigidbody2D  _playerRb;
    private TongueRenderer _customRenderer; // si existe, él manda en lo visual

    private Vector2 _tongueDir;
    private Vector2 _tongueTip;
    private float   _currentLength;

    // Péndulo
    private Vector2 _anchorPoint;
    private float   _ropeLength;       // longitud fija de la cuerda
    private float   _swingTimer;

    // Cooldown / uso
    private float   _cooldownTimer;
    private bool    _usedInAir;
    private bool    _wasGrounded;

    // Cache para no alocar arrays cada frame en el raycast combinado
    private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _line  = GetComponent<LineRenderer>();
        _audio = gameObject.GetComponent<AudioSource>()
              ?? gameObject.AddComponent<AudioSource>();

        if (tongueOrigin     == null) tongueOrigin     = transform;
        if (playerController == null) playerController = GetComponent<PlayerController>();

        _playerRb = GetComponent<Rigidbody2D>();

        _line.positionCount = tongueSegments;

        _customRenderer = GetComponent<TongueRenderer>();

        // FIX: si el objeto tiene un TongueRenderer (o cualquier script que
        // configure su propio color/ancho/material/gradiente), TongueGrab
        // NO debe tocar esas propiedades — dos Awake() en el mismo objeto
        // configurando el mismo LineRenderer pelean entre sí en un orden que
        // Unity no garantiza, y eso es lo que suele hacer que "no se vea".
        if (_customRenderer == null)
        {
            if (_line.sharedMaterial == null)
            {
                _line.material = new Material(Shader.Find("Sprites/Default"));
            }
            _line.startColor       = tongueColorBase;
            _line.endColor         = tongueColorTip;
            _line.startWidth       = tongueWidthBase;
            _line.endWidth         = tongueWidthTip;
            _line.sortingLayerName = sortingLayerName;
            _line.sortingOrder     = sortingOrder;
        }

        // La lengua queda siempre habilitada: en Idle se dibuja un pico corto
        // en reposo (si showIdleTongue está activo); al usarla, se extiende igual.
        _line.enabled = showIdleTongue;
    }

    private void Update()
    {
        TickCooldown();
        HandleGroundRecharge();

        // Input
        bool pressed  = TongueButtonDown();
        bool jumpDown = Input.GetButtonDown("Jump") ||
                        (UnityEngine.InputSystem.Gamepad.current != null &&
                         UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame);
        bool held     = TongueButtonHeld();

        switch (State)
        {
            case TongueState.Idle:
                UpdateIdleTongue();
                if (pressed) TryStartTongue();
                break;

            case TongueState.Extending:
                ExtendTongue();
                break;

            case TongueState.Swinging:
                UpdateSwing(held, jumpDown);
                break;

            case TongueState.Retracting:
                RetractTongue();
                break;
        }

        UpdateLineRenderer();
    }

    // La física del péndulo va en FixedUpdate para estabilidad
    private void FixedUpdate()
    {
        if (State != TongueState.Swinging) return;
        ApplyPendulumConstraint();
    }

    // ── Input helpers ─────────────────────────────────────────────────

    private bool TongueButtonDown()
    {
        var gp = UnityEngine.InputSystem.Gamepad.current;
        return Input.GetButtonDown("Fire2") ||
               (gp != null && gp.buttonEast.wasPressedThisFrame);
    }

    private bool TongueButtonHeld()
    {
        var gp = UnityEngine.InputSystem.Gamepad.current;
        return Input.GetButton("Fire2") ||
               (gp != null && gp.buttonEast.isPressed);
    }

    // ── Cooldown & recarga ────────────────────────────────────────────

    private void TickCooldown()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f) PlaySfx(sfxReady);
        }
    }

    private void HandleGroundRecharge()
    {
        if (!rechargeOnGround) return;
        bool grounded = IsGrounded();
        if (grounded && !_wasGrounded) _usedInAir = false;
        _wasGrounded = grounded;
    }

    // ── Inicio ───────────────────────────────────────────────────────

    private void TryStartTongue()
    {
        if (_cooldownTimer > 0f) return;
        if (rechargeOnGround && !IsGrounded() && _usedInAir) return;
        StartTongue();
    }

    private void StartTongue()
    {
        float inputY = Input.GetAxisRaw("Vertical");
        float dirX   = transform.localScale.x > 0 ? 1f : -1f;

        _tongueDir = inputY >  0.5f ? new Vector2(dirX * 0.4f,  1f).normalized   // casi recto arriba
                   : inputY < -0.5f ? new Vector2(dirX * 0.7f, -0.7f).normalized
                   :                  new Vector2(dirX, 0f);

        _tongueTip     = tongueOrigin.position;
        _currentLength = 0f;
        State          = TongueState.Extending;
        _line.enabled  = true;
        PlaySfx(sfxExtend);
    }

    // ── Extensión ────────────────────────────────────────────────────

    private void ExtendTongue()
    {
        _currentLength += tongueSpeed * Time.deltaTime;
        _tongueTip      = (Vector2)tongueOrigin.position + _tongueDir * _currentLength;

        // FIX: antes se hacían dos CircleCast separados (enemigo y superficie) y
        // siempre "ganaba" el enemigo aunque una pared estuviera más cerca. Ahora
        // se combinan ambas capas en un solo cast y se toma el impacto MÁS CERCANO,
        // sin importar a qué capa pertenezca.
        int hitCount = Physics2D.CircleCastNonAlloc(
            tongueOrigin.position, 0.18f, _tongueDir, _hitBuffer,
            _currentLength, grabLayer | surfaceLayer);

        if (hitCount > 0)
        {
            RaycastHit2D closest = _hitBuffer[0];
            for (int i = 1; i < hitCount; i++)
            {
                if (_hitBuffer[i].fraction < closest.fraction)
                    closest = _hitBuffer[i];
            }

            _tongueTip = closest.point;

            bool isEnemy = ((1 << closest.collider.gameObject.layer) & grabLayer.value) != 0;
            if (isEnemy)
                OnHitEnemy(closest.collider);
            else
                OnHitSurface(closest.point);
            return;
        }

        if (_currentLength >= maxTongueLength)
            StartRetract();
    }

    // ── Golpe a enemigo ───────────────────────────────────────────────

    private void OnHitEnemy(Collider2D col)
    {
        PlaySfx(sfxHitEnemy);

        EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
        if (enemy != null) enemy.TakeDamage(tongueDamage);

        if (_playerRb != null)
        {
            Vector2 dir = (_tongueTip - (Vector2)transform.position).normalized;
            _playerRb.linearVelocity = new Vector2(_playerRb.linearVelocity.x, 0f);
            _playerRb.AddForce(dir * enemyLaunchForce, ForceMode2D.Impulse);
        }

        // "Tragar" al enemigo: cura al jugador (efecto rana-lengua).
        if (healPerEnemyHit > 0f && playerController != null)
            playerController.Heal(healPerEnemyHit);

        ConsumeTongue();
        StartRetract();
    }

    // ── Enganche a superficie → péndulo ──────────────────────────────

    private void OnHitSurface(Vector2 contactPoint)
    {
        PlaySfx(sfxHitSurface);

        _anchorPoint = contactPoint;
        // La longitud de la cuerda es la distancia real al contacto
        _ropeLength  = Vector2.Distance(transform.position, contactPoint);
        _ropeLength  = Mathf.Max(_ropeLength, 0.5f);   // mínimo para evitar degeneración
        _swingTimer  = 0f;

        State = TongueState.Swinging;
        ConsumeTongue();
    }

    // ── Péndulo ───────────────────────────────────────────────────────

    /// <summary>
    /// Constraint de cuerda rígida, estable (no basado en fuerzas de resorte).
    /// Cada FixedUpdate: si el jugador se pasó del radio de la cuerda, se lo
    /// clava exactamente sobre el círculo y se descarta la velocidad radial,
    /// dejando solo la componente TANGENCIAL. La gravedad de Unity sigue
    /// actuando normal en todo momento — no hay ninguna fuerza "tirando" que
    /// pueda generar rebotes u oscilaciones locas.
    /// </summary>
    private void ApplyPendulumConstraint()
    {
        if (_playerRb == null) return;

        Vector2 toPlayer = _playerRb.position - _anchorPoint;
        float   dist     = toPlayer.magnitude;

        if (dist > _ropeLength && dist > 0.0001f)
        {
            Vector2 radial = toPlayer / dist;

            // Corrección posicional dura: nunca te alejás más del radio de la cuerda.
            _playerRb.position = _anchorPoint + radial * _ropeLength;

            // Solo se conserva la velocidad TANGENCIAL (perpendicular a la cuerda).
            // Esto es lo que hace que se sienta como un péndulo real: la cuerda no
            // "tira" con fuerza, simplemente no te deja alejar más de lo que mide.
            Vector2 tangent         = new Vector2(-radial.y, radial.x);
            float   tangentialSpeed = Vector2.Dot(_playerRb.linearVelocity, tangent);
            _playerRb.linearVelocity = tangent * tangentialSpeed;
        }

        // Damping suave (resistencia del aire), sin anular la gravedad real.
        float speed = _playerRb.linearVelocity.magnitude;
        float drag  = swingDamping * (1f + speed * 0.03f);
        _playerRb.linearVelocity *= Mathf.Max(0f, 1f - drag);
    }

    /// <summary>
    /// Input durante el swing: columpio activo + detección de salto/suelta.
    /// </summary>
    private void UpdateSwing(bool tongueHeld, bool jumpPressed)
    {
        _swingTimer += Time.deltaTime;

        // Dibujar lengua hacia el ancla
        _tongueTip = _anchorPoint;

        // FIX: si el personaje toca el suelo mientras sigue "colgado", el péndulo
        // seguía empujándolo contra el piso de forma rara. Ahora se suelta solo.
        if (IsGrounded())
        {
            LaunchFromGrapple(isJump: false);
            return;
        }

        // ── Saltar desde el gancho ────────────────────────────────────
        if (jumpPressed)
        {
            LaunchFromGrapple(isJump: true);
            return;
        }

        // ── Soltar botón o tiempo agotado ─────────────────────────────
        if (!tongueHeld || _swingTimer >= maxSwingTime)
        {
            LaunchFromGrapple(isJump: false);
            return;
        }

        // ── Columpio activo con input ─────────────────────────────────
        float inputX = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(inputX) > 0.1f && _playerRb != null)
        {
            Vector2 toAnchor = _anchorPoint - (Vector2)transform.position;
            Vector2 tangent  = new Vector2(-toAnchor.normalized.y, toAnchor.normalized.x);

            if (Vector2.Dot(tangent, Vector2.right) * inputX < 0f)
                tangent = -tangent;

            float velOnTangent = Vector2.Dot(_playerRb.linearVelocity, tangent);
            float pump         = velOnTangent > 0f ? 1.5f : 0.75f;

            _playerRb.AddForce(tangent * swingInputForce * pump, ForceMode2D.Force);
        }
    }

    /// <summary>
    /// Lanzamiento al soltar el gancho.
    /// Conserva la velocidad tangencial del péndulo y añade boost.
    /// Si es salto, añade un impulso vertical extra.
    /// </summary>
    private void LaunchFromGrapple(bool isJump)
    {
        if (_playerRb != null)
        {
            Vector2 vel = _playerRb.linearVelocity;

            if (isJump)
            {
                _playerRb.linearVelocity = new Vector2(vel.x * releaseBoost, 0f);
                _playerRb.AddForce(Vector2.up * jumpFromGrapple, ForceMode2D.Impulse);
            }
            else
            {
                float speed = Mathf.Max(vel.magnitude * releaseBoost, minReleaseSpeed);
                Vector2 dir = vel.magnitude > 0.1f ? vel.normalized : _tongueDir;
                _playerRb.linearVelocity = dir * speed;
            }
        }

        StartRetract();
    }

    // ── Retracción ────────────────────────────────────────────────────

    private void StartRetract()
    {
        State          = TongueState.Retracting;
        _currentLength = Vector2.Distance(tongueOrigin.position, _tongueTip);
    }

    private void RetractTongue()
    {
        _currentLength -= tongueRetractSpeed * Time.deltaTime;
        _tongueTip      = (Vector2)tongueOrigin.position
                        + _tongueDir * Mathf.Max(0f, _currentLength);

        if (_currentLength <= 0f) ResetTongue();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void ConsumeTongue()
    {
        _cooldownTimer = cooldown;
        if (!IsGrounded()) _usedInAir = true;
    }

    /// <summary>
    /// Cuando no se está usando la lengua, muestra un pico corto asomando
    /// de la boca con un balanceo suave (efecto "reptil en reposo").
    /// </summary>
    private void UpdateIdleTongue()
    {
        if (!showIdleTongue)
        {
            _line.enabled = false;
            return;
        }

        _line.enabled = true;

        float dirX = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 restDir = new Vector2(dirX, -0.15f).normalized;

        // Pequeño balanceo vertical/horizontal para que no se vea estática
        float wiggle = Mathf.Sin(Time.time * idleWiggleSpeed) * 0.05f;
        Vector2 perpWiggle = new Vector2(0f, wiggle);

        _tongueDir     = restDir;
        _currentLength = idleTongueLength;
        _tongueTip     = (Vector2)tongueOrigin.position + restDir * idleTongueLength + perpWiggle;
    }

    private void ResetTongue()
    {
        _currentLength = 0f;
        State          = TongueState.Idle;
        // No apagamos el LineRenderer aquí: UpdateIdleTongue() decide su visibilidad
        // en el próximo Update() según showIdleTongue.
    }

    private bool IsGrounded()
    {
        if (_playerRb == null) return false;
        return Physics2D.OverlapCircle(
            _playerRb.position + Vector2.down * groundCheckOffset, groundCheckRadius, surfaceLayer);
    }

    private void UpdateLineRenderer()
    {
        if (!_line.enabled) return;

        if (_line.positionCount != tongueSegments)
            _line.positionCount = tongueSegments;

        Vector2 origin = tongueOrigin.position;
        Vector2 tip    = _tongueTip;
        Vector2 along  = tip - origin;
        float   length = along.magnitude;

        Vector2 perp   = along.magnitude > 0.001f
                       ? new Vector2(-along.normalized.y, along.normalized.x)
                       : Vector2.up;

        float speedAmp = _playerRb != null
                       ? _playerRb.linearVelocity.magnitude * waveVelocityInfluence
                       : 0f;
        float amp = waveAmplitude + speedAmp;

        if (State == TongueState.Swinging)
            amp *= 1.4f;

        for (int i = 0; i < tongueSegments; i++)
        {
            float t = (float)i / (tongueSegments - 1);

            Vector2 basePos = Vector2.Lerp(origin, tip, t);

            float taper  = Mathf.Sin(t * Mathf.PI) * waveTaper
                         + (1f - waveTaper);

            float wave = Mathf.Sin(
                t * length * waveFrequency
                - Time.time * waveSpeed
            ) * amp * taper;

            Vector2 pos = basePos + perp * wave;
            _line.SetPosition(i, pos);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && _audio != null)
            _audio.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        if (tongueOrigin == null) return;
        Gizmos.color = _cooldownTimer > 0f ? Color.yellow : Color.red;
        Gizmos.DrawWireSphere(tongueOrigin.position, 0.18f);

        if (State == TongueState.Swinging)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_anchorPoint, 0.12f);
            Gizmos.DrawLine(_anchorPoint, transform.position);
        }

        if (_playerRb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                Application.isPlaying ? _playerRb.position + Vector2.down * groundCheckOffset
                                      : (Vector2)transform.position + Vector2.down * groundCheckOffset,
                groundCheckRadius);
        }
    }
}

public enum TongueState { Idle, Extending, Swinging, Retracting }
