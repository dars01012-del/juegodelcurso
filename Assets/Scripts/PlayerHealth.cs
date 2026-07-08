using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 5;

    [Header("Efecto de Daño")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashTime = 0.1f;

    private int currentHealth;
    private bool isDead;

    private PlayerController playerController;
    private Color originalColor;

    private void Awake()
    {
        currentHealth = maxHealth;

        playerController = GetComponent<PlayerController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage, Vector2 enemyPosition)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        Debug.Log("Vida jugador: " + currentHealth);

        // Retroceso
        if (playerController != null)
        {
            playerController.ApplyKnockback(enemyPosition);
        }

        // Hit Flash
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitColor;

            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), hitFlashTime);
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
        isDead = true;

        CancelInvoke(nameof(ResetColor));

        Debug.Log("Jugador muerto");

        Destroy(gameObject);
    }
}