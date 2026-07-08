using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;

    [Header("Efecto de Daño")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    private int currentHealth;
    private Color originalColor;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log(gameObject.name + " Vida: " + currentHealth);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitColor;

            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), flashDuration);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        CancelInvoke(nameof(ResetColor));

        Debug.Log(gameObject.name + " murió");

        Destroy(gameObject);
    }
}