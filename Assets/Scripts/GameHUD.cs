using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private Text coinText;
    [SerializeField] private Text healthText;

    private PlayerHealth playerHealth;

    private void Start()
    {
        EnsureUI();

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
            UpdateCoinDisplay(CoinManager.Instance.TotalCoins);
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthDisplay;
            UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;

        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
    }

    private void EnsureUI()
    {
        if (coinText != null && healthText != null)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HUD_Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (coinText == null)
            coinText = CreateText(canvas.transform, "Monedas: 0", new Vector2(20f, -20f), new Color(1f, 0.9f, 0.2f));

        if (healthText == null)
            healthText = CreateText(canvas.transform, "Vida: 5 / 5", new Vector2(20f, -55f), new Color(1f, 0.35f, 0.35f));
    }

    private Text CreateText(Transform parent, string label, Vector2 anchoredPosition, Color color)
    {
        GameObject textObject = new GameObject(label.Split(':')[0] + "Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(320f, 40f);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.UpperLeft;
        text.color = color;
        text.text = label;

        return text;
    }

    private void UpdateCoinDisplay(int total)
    {
        if (coinText != null)
            coinText.text = "Monedas: " + total;
    }

    private void UpdateHealthDisplay(int current, int max)
    {
        if (healthText != null)
            healthText.text = "Vida: " + current + " / " + max;
    }
}
