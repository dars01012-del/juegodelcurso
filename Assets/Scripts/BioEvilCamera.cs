using UnityEngine;

public class BioEvilCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offset")]
    public float forwardOffset = 2.5f;
    public float verticalOffset = 0.8f;
    public float smooth = 4f;

    [Header("Dead Zone")]
    public float deadZoneX = 1.2f;
    public float deadZoneY = 0.6f;

    [Header("Bounds")]
    public Collider2D mapBounds;

    private Camera cam;
    private Vector3 velocity;
    private Vector3 shakeOffset;

    float currentOffset;

    void Awake()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (target == null) return;

        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            float desiredOffset = sr.flipX ? -forwardOffset : forwardOffset;
            currentOffset = Mathf.Lerp(currentOffset, desiredOffset, Time.deltaTime * 5);
        }

        Vector3 desired = transform.position;

        float dx = target.position.x - transform.position.x - currentOffset;

        if (Mathf.Abs(dx) > deadZoneX)
            desired.x += dx - Mathf.Sign(dx) * deadZoneX;

        float dy = target.position.y - transform.position.y - verticalOffset;

        if (Mathf.Abs(dy) > deadZoneY)
            desired.y += dy - Mathf.Sign(dy) * deadZoneY;

        desired.z = -10;

        desired += shakeOffset;

        Clamp(ref desired);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            1f / smooth);
    }

    void Clamp(ref Vector3 pos)
    {
        if (mapBounds == null)
            return;

        Bounds b = mapBounds.bounds;

        float h = cam.orthographicSize;
        float w = h * cam.aspect;

        pos.x = Mathf.Clamp(pos.x, b.min.x + w, b.max.x - w);
        pos.y = Mathf.Clamp(pos.y, b.min.y + h, b.max.y - h);
    }

    public void Shake(float intensity,float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(intensity,duration));
    }

    System.Collections.IEnumerator ShakeRoutine(float intensity,float duration)
    {
        float t = 0;

        while(t<duration)
        {
            shakeOffset = Random.insideUnitCircle * intensity;
            t += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }
}