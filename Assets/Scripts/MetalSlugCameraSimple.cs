using System.Collections;
using UnityEngine;

public class MetalSlugCameraSimple : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 1, -10);

    [Header("Follow")]
    public float smoothTime = 0.12f;

    [Header("Look Ahead")]
    public float lookAheadDistance = 2.5f;
    public float lookSmooth        = 5f;

    [Header("Map Bounds (Collider2D)")]
    public Collider2D mapBounds;

    // ── Shake ────────────────────────────────────────────────────────
    private Vector3 shakeOffset;

    private Vector3 velocity;
    private float   currentLookAhead;
    private float   targetLookAhead;
    private Camera  cam;

    // ── Lifecycle ────────────────────────────────────────────────────

    void Start()
    {
        cam = Camera.main;
        TryFindPlayer();
    }

    void LateUpdate()
    {
        // Si perdimos al jugador (murió) intentar encontrar el nuevo
        if (target == null)
            TryFindPlayer();

        // Sin target ni bounds: no hacer nada (cámara quieta)
        if (target == null || mapBounds == null) return;

        HandleLookAhead();

        Vector3 desiredPosition = target.position
                                + offset
                                + new Vector3(currentLookAhead, 0, 0)
                                + shakeOffset;

        ClampToMap(ref desiredPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime);
    }

    // ── Buscar jugador por Tag ────────────────────────────────────────

    /// <summary>
    /// Busca el jugador en escena por Tag "Player".
    /// Se llama al inicio y cada frame que target == null.
    /// </summary>
    private void TryFindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    // ── Look Ahead ────────────────────────────────────────────────────

    void HandleLookAhead()
    {
        float inputX = Input.GetAxisRaw("Horizontal");

        targetLookAhead  = Mathf.Abs(inputX) > 0.1f ? inputX * lookAheadDistance : 0f;
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookSmooth);
    }

    // ── Clamp ─────────────────────────────────────────────────────────

    void ClampToMap(ref Vector3 pos)
    {
        Bounds b = mapBounds.bounds;

        float camHeight = cam.orthographicSize;
        float camWidth  = camHeight * cam.aspect;

        pos.x = Mathf.Clamp(pos.x, b.min.x + camWidth,  b.max.x - camWidth);
        pos.y = Mathf.Clamp(pos.y, b.min.y + camHeight, b.max.y - camHeight);
    }

    // ── Shake API ─────────────────────────────────────────────────────

    public void Shake(float intensity, float duration)
    {
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            shakeOffset   = Random.insideUnitSphere * intensity;
            shakeOffset.z = 0f;
            t            += Time.deltaTime;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }

    // ── API pública para el RespawnManager ───────────────────────────

    /// <summary>
    /// Llamado por RespawnManager tras instanciar al jugador
    /// para asignar el target directamente sin esperar un frame.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target   = newTarget;
        velocity = Vector3.zero;   // resetear SmoothDamp para evitar salto brusco
    }
}
