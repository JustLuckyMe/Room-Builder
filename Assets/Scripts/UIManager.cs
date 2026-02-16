using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private PlayerBalance playerBalance;

    private void OnEnable()
    {
        if (playerBalance != null)
            playerBalance.OnBalanceChanged += HandleBalanceChanged;
    }

    private void OnDisable()
    {
        if (playerBalance != null)
            playerBalance.OnBalanceChanged -= HandleBalanceChanged;
    }

    private void Start()
    {
        if (playerBalance != null)
            HandleBalanceChanged(playerBalance.GetBalance());
    }

    private void HandleBalanceChanged(int newBalance)
    {
        if (balanceText != null)
            balanceText.text = newBalance.ToString();
    }
}