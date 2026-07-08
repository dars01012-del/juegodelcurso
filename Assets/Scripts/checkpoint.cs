using UnityEngine;

/// <summary>
/// Checkpoint — Al tocarlo el jugador guarda su posición de respawn.
///
/// Setup:
///   1. Crea un GameObject vacío en el nivel, añade este script.
///   2. Añade un Collider2D (ej: BoxCollider2D) y marca Is Trigger = true.
///   3. (Opcional) Asigna un Animator con los estados "Idle" y "Active".
///   4. El jugador debe tener el Tag "Player".
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Animator opcional — debe tener un parámetro Bool 'Active'.")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject activateFXPrefab;   // partículas al activarse

    [Header("Audio")]
    [SerializeField] private AudioClip activateSfx;

    // ── Estado ───────────────────────────────────────────────────────
    public bool IsActive { get; private set; }

    private static readonly int HashActive = Animator.StringToHash("Active");

    // ── Trigger ──────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsActive) return;
        if (!other.CompareTag("Player")) return;

        Activate();
        RespawnManager.Instance?.SetCheckpoint(this);
    }

    // ── Activación ───────────────────────────────────────────────────
    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;

        if (animator != null)
            animator.SetBool(HashActive, true);

        if (activateFXPrefab != null)
        {
            var fx = Instantiate(activateFXPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (activateSfx != null)
            AudioSource.PlayClipAtPoint(activateSfx, transform.position);
    }

    /// <summary>Posición de spawn: el pivot del checkpoint.</summary>
    public Vector3 SpawnPosition => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = IsActive ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
