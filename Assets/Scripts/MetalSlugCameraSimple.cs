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
    public float lookSmooth = 5f;

    [Header("Map Bounds (Collider2D)")]
    public Collider2D mapBounds;

    [Header("Shake")]
    private Vector3 shakeOffset;

    private Vector3 velocity;
    private float currentLookAhead;
    private float targetLookAhead;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!target || !mapBounds) return;

        HandleLookAhead();

        Vector3 basePos = target.position + offset;

        Vector3 desiredPosition =
            basePos +
            new Vector3(currentLookAhead, 0, 0) +
            shakeOffset;

        ClampToMap(ref desiredPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }

    void HandleLookAhead()
    {
        float inputX = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(inputX) > 0.1f)
            targetLookAhead = inputX * lookAheadDistance;
        else
            targetLookAhead = 0;

        currentLookAhead = Mathf.Lerp(
            currentLookAhead,
            targetLookAhead,
            Time.deltaTime * lookSmooth
        );
    }

    void ClampToMap(ref Vector3 pos)
    {
        Bounds b = mapBounds.bounds;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        pos.x = Mathf.Clamp(pos.x, b.min.x + camWidth, b.max.x - camWidth);
        pos.y = Mathf.Clamp(pos.y, b.min.y + camHeight, b.max.y - camHeight);
    }

    // 💥 API de shake (explosiones)
    public void Shake(float intensity, float duration)
    {
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    System.Collections.IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float t = 0;

        while (t < duration)
        {
            shakeOffset = Random.insideUnitSphere * intensity;
            shakeOffset.z = 0;

            t += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }
}