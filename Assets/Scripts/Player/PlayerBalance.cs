using System;
using UnityEngine;

public class PlayerBalance : MonoBehaviour
{
    public event Action<int> OnBalanceChanged;

    [SerializeField] private int currentBalance = 1000;

    public int GetBalance()
    {
        return currentBalance;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0)
            return true;

        if (currentBalance >= amount)
        {
            currentBalance -= amount;
            OnBalanceChanged?.Invoke(currentBalance);
            return true;
        }

        return false;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        currentBalance += amount;
        OnBalanceChanged?.Invoke(currentBalance);
    }
}