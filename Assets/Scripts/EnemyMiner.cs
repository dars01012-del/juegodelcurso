using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyMiner — Torreta estática que lanza su arma hacia el frente
/// cuando el jugador entra en rango. No se mueve.
///
/// ANIMATOR PARAMETERS:
///   Trigger: Throw
///   Trigger: Hurt
///   Trigger: Die
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyMiner : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Lanzamiento")]
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private Transform  throwPoint;
    [SerializeField] private float      throwWindupTime = 0.4f;
    [SerializeField] private float      throwCooldown   = 2f;
    [SerializeField] private float      throwForce      = 12f;

    [Header("Daño por contacto")]
    [SerializeField] private int   contactDamage   = 1;
    [SerializeField] private float contactCooldown = 0.8f;

    [Header("Vida")]
    [SerializeField] private int   maxHealth   = 3;
    [SerializeField] private float hitFlashTime = 0.12f;
    [SerializeField] private float deathDelay   = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxThrow;
    [SerializeField] private AudioClip sfxHurt;
    [SerializeField] private AudioClip sfxDie;

    [Header("Referencias")]
    [SerializeField] private Animator       animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // ── Estado ───────────────────────────────────────────────────────

    private enum State { Idle, Windup, Dead }
    private State _state = State.Idle;

    private Rigidbody2D _rb;
    private AudioSource _audio;
    private Transform   _player;
    private Color       _originalColor;

    private int   _currentHealth;
    private float _throwCooldownTimer;
    private float _contactCooldownTimer;
    private float _windupTimer;

    // Dirección fija hacia la que dispara (se define por el scale del sprite)
    private int _facing => transform.localScale.x > 0 ? 1 : -1;

    private static readonly int HashThrow = Animator.StringToHash("Throw");
    private static readonly int HashHurt  = Animator.StringToHash("Hurt");
    private static readonly int HashDie   = Animator.StringToHash("Die");

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _rb            = GetComponent<Rigidbody2D>();
        _rb.bodyType   = RigidbodyType2D.Static; // torreta: no se mueve por física
        _audio         = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        _currentHealth = maxHealth;

        if (animator       == null) animator       = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _originalColor = spriteRenderer.color;
        if (throwPoint     == null) throwPoint     = transform;
    }

    private void Update()
    {
        if (_state == State.Dead) return;

        FindPlayer();
        TickTimers();

        switch (_state)
        {
            case State.Idle:   DoIdle();   break;
            case State.Windup: DoWindup(); break;
        }
    }

    // ── Buscar jugador ────────────────────────────────────────────────

    private void FindPlayer()
    {
        if (_player != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    // ── Timers ───────────────────────────────────────────────────────

    private void TickTimers()
    {
        _throwCooldownTimer   = Mathf.Max(0f, _throwCooldownTimer   - Time.deltaTime);
        _contactCooldownTimer = Mathf.Max(0f, _contactCooldownTimer - Time.deltaTime);
    }

    // ── Idle: esperar al jugador ──────────────────────────────────────

    private void DoIdle()
    {
        if (_player == null) return;
        if (_throwCooldownTimer > 0f) return;

        // Detectar si el jugador está al frente y en rango
        float dist   = Vector2.Distance(transform.position, _player.position);
        float dirToPlayer = _player.position.x - transform.position.x;
        bool  inFront = Mathf.Sign(dirToPlayer) == _facing;

        if (dist <= detectionRange && inFront)
            StartWindup();
    }

    // ── Windup: carga antes de lanzar ─────────────────────────────────

    private void DoWindup()
    {
        _windupTimer -= Time.deltaTime;
        if (_windupTimer <= 0f)
            ExecuteThrow();
    }

    private void StartWindup()
    {
        _state       = State.Windup;
        _windupTimer = throwWindupTime;
        // Aquí puedes setear un trigger "Windup" en el animator si tienes esa animación
    }

    // ── Lanzar arma ───────────────────────────────────────────────────

    private void ExecuteThrow()
    {
        _state = State.Idle;
        _throwCooldownTimer = throwCooldown;

        if (weaponPrefab == null) return;

        animator?.SetTrigger(HashThrow);
        PlaySfx(sfxThrow);

        // Dirección fija hacia el frente del sprite, ligeramente hacia arriba
        float angle = _facing > 0 ? 10f : 170f;   // 10° arriba al frente
        Vector2 dir = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad));

        GameObject weapon = Instantiate(weaponPrefab, throwPoint.position, Quaternion.identity);
        weapon.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (weapon.TryGetComponent<Rigidbody2D>(out var rb))
            rb.linearVelocity = dir * throwForce;

        if (weapon.TryGetComponent<MinerWeaponProjectile>(out var proj))
            proj.Init(contactDamage + 1);
    }

    // ── Daño recibido ─────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (_state == State.Dead) return;

        _currentHealth -= damage;
        PlaySfx(sfxHurt);
        animator?.SetTrigger(HashHurt);
        StartCoroutine(FlashHurt());

        if (_currentHealth <= 0) Die();
    }

    private void Die()
    {
        _state = State.Dead;
        PlaySfx(sfxDie);
        animator?.SetTrigger(HashDie);

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        Destroy(gameObject, deathDelay);
    }

    private IEnumerator FlashHurt()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(hitFlashTime);
        if (spriteRenderer != null) spriteRenderer.color = _originalColor;
    }

    // ── Daño por contacto ─────────────────────────────────────────────

    private void OnCollisionStay2D(Collision2D col)
    {
        if (_state == State.Dead || _contactCooldownTimer > 0f) return;
        if (!col.gameObject.CompareTag("Player")) return;

        col.gameObject.GetComponent<PlayerHealth>()
            ?.TakeDamage(contactDamage, transform.position);
        _contactCooldownTimer = contactCooldown;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && _audio != null) _audio.PlayOneShot(clip);
    }

    private void OnDrawGizmosSelected()
    {
        // Rango de detección
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Dirección de disparo
        int facing = transform.localScale.x > 0 ? 1 : -1;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position,
            transform.position + new Vector3(facing * detectionRange, 0.3f));
    }
}
