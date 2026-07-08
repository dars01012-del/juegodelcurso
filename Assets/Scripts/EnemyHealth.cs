using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    private int currentHealth;
    private Color originalColor;
    private bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int> OnDamaged;

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
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;
        OnDamaged?.Invoke(damage);

        Flash();

        if (currentHealth <= 0)
            Die();
    }

    private void Flash()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = hitColor;
        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), flashDuration);
    }

    private void ResetColor()
    {
        if (spriteRenderer != null && !isDead)
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        CancelInvoke(nameof(ResetColor));

        EnemyBatAI batAI = GetComponent<EnemyBatAI>();
        if (batAI != null)
        {
            batAI.HandleDeath();
            return;
        }

        Destroy(gameObject);
    }
}
