using UnityEngine;

public class PlayerBalance : MonoBehaviour
{
    [SerializeField] private int currentBalance = 1000;

    public int GetBalance()
    {
        return currentBalance;
    }

    public bool TrySpend(int amount)
    {
        if (currentBalance >= amount)
        {
            currentBalance -= amount;
            return true;
        }

        return false;
    }

    public void AddMoney(int amount)
    {
        currentBalance += amount;
    }
}