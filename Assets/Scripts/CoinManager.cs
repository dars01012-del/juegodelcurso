using System;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    public int TotalCoins { get; private set; }

    public event Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        TotalCoins += amount;
        OnCoinsChanged?.Invoke(TotalCoins);
    }
}
