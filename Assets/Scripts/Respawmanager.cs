using System.Collections;
using UnityEngine;

/// <summary>
/// RespawnManager — Singleton que guarda el checkpoint activo
/// y gestiona el respawn del jugador.
///
/// Coloca este script en un GameObject persistente en la escena.
/// </summary>
public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Jugador")]
    [Tooltip("Prefab del jugador para reinstanciar al morir.")]
    [SerializeField] private GameObject playerPrefab;
    [Tooltip("Posición de spawn inicial si no se ha tocado ningún checkpoint.")]
    [SerializeField] private Transform  defaultSpawnPoint;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay    = 1.5f;
    [Tooltip("Prefab de efecto al aparecer (opcional).")]
    [SerializeField] private GameObject spawnFXPrefab;
    [SerializeField] private AudioClip  respawnSfx;

    // ── Estado ───────────────────────────────────────────────────────
    private Checkpoint _activeCheckpoint;
    private bool       _isRespawning;

    // ── Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Instanciar al jugador al inicio de la escena
        if (playerPrefab != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, GetSpawnPosition(), Quaternion.identity);

            MetalSlugCameraSimple cam = Camera.main?.GetComponent<MetalSlugCameraSimple>();
            if (cam != null)
                cam.SetTarget(newPlayer.transform);
        }
        else
            Debug.LogWarning("[RespawnManager] playerPrefab no asignado.");
    }

    // ── API pública ───────────────────────────────────────────────────

    /// <summary>Llamado por Checkpoint cuando el jugador lo toca.</summary>
    public void SetCheckpoint(Checkpoint checkpoint)
    {
        _activeCheckpoint = checkpoint;
        Debug.Log($"[RespawnManager] Checkpoint guardado: {checkpoint.name}");
    }

    /// <summary>
    /// Llamado por PlayerHealth.Die() para iniciar el respawn.
    /// </summary>
    public void RequestRespawn()
    {
        if (_isRespawning) return;
        StartCoroutine(RespawnRoutine());
    }

    // ── Respawn ───────────────────────────────────────────────────────
    private IEnumerator RespawnRoutine()
    {
        _isRespawning = true;

        // Esperar antes de aparecer (tiempo para animación de muerte, fade, etc.)
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = GetSpawnPosition();

        // Efecto visual al aparecer
        if (spawnFXPrefab != null)
        {
            var fx = Instantiate(spawnFXPrefab, spawnPos, Quaternion.identity);
            Destroy(fx, 2f);
        }

        if (respawnSfx != null)
            AudioSource.PlayClipAtPoint(respawnSfx, spawnPos);

        // Instanciar jugador
        if (playerPrefab != null)
        {
            GameObject newPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

            // Notificar a la cámara inmediatamente para que no espere un frame
            MetalSlugCameraSimple cam = Camera.main?.GetComponent<MetalSlugCameraSimple>();
            if (cam != null)
                cam.SetTarget(newPlayer.transform);
        }
        else
            Debug.LogWarning("[RespawnManager] playerPrefab no asignado.");

        _isRespawning = false;
    }

    private Vector3 GetSpawnPosition()
    {
        if (_activeCheckpoint != null)
            return _activeCheckpoint.SpawnPosition;

        if (defaultSpawnPoint != null)
            return defaultSpawnPoint.position;

        Debug.LogWarning("[RespawnManager] Sin checkpoint ni defaultSpawnPoint. Respawn en (0,0,0).");
        return Vector3.zero;
    }
}
