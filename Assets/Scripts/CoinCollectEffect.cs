using UnityEngine;

public class CoinCollectEffect : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.35f;
    [SerializeField] private float expandSpeed = 3f;
    [SerializeField] private float fadeSpeed = 4f;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private Vector3 startScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;

        transform.localScale = startScale * (1f + t * expandSpeed);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(1f, 0f, t * fadeSpeed);
            spriteRenderer.color = color;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
