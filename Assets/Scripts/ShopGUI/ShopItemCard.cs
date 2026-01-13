using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemCard : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;

    private ShopItemSO currentItem;
    private ShopManager shopManager;

    // Called from ShopUI
    public void Setup(ShopItemSO item)
    {
        currentItem = item;

        if (shopManager == null)
        {
            shopManager = FindObjectOfType<ShopManager>();
        }

        if (icon != null) icon.sprite = item.icon;
        if (nameText != null) nameText.text = item.itemName;
        if (priceText != null) priceText.text = item.price.ToString();
    }

    public void OnBuyClicked()
    {
        if (currentItem == null || shopManager == null)
        {
            Debug.LogWarning("ShopItemCard missing item or shopManager reference.");
            return;
        }

        bool bought = shopManager.TryBuy(currentItem);

        if (bought)
        {
            // Optional: you can disable the button or show "Owned"
            // buyButton.interactable = false;
        }
    }
}