using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashTime = 0.1f;

    private int currentHealth;
    private bool isDead;
    private PlayerController playerController;
    private Color originalColor;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int, int> OnHealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage, Vector2 enemyPosition)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (playerController != null)
            playerController.ApplyKnockback(enemyPosition);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitColor;
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), hitFlashTime);
        }

        if (currentHealth <= 0)
            Die();
    }

    private void ResetColor()
    {
        if (spriteRenderer != null && !isDead)
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        isDead = true;
        CancelInvoke(nameof(ResetColor));
        Destroy(gameObject);
    }
}
