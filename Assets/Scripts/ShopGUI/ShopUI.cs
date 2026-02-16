using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject shopGUI;

    [Header("Tabs")]
    [SerializeField]
    private string[] tabs =
    {
        "Seating",
        "Surfaces",
        "Storage",
        "Beds",
        "Kitchen",
        "Bathroom",
        "Office",
        "Decorative",
        "Lighting",
        "Appliances"
    };

    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Items UI")]
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private Transform itemsParent;
    [SerializeField] private ShopItemCard itemCardPrefab;

    private int currentTab = 0;
    private bool isOpen;

    private void Start()
    {
        isOpen = false;
        if (shopGUI != null)
        {
            shopGUI.SetActive(false);
        }

        UpdateUI();
    }

    public void ToggleShop()
    {
        isOpen = !isOpen;

        if (shopGUI != null)
        {
            shopGUI.SetActive(isOpen);
        }

        // Optional: refresh items when opening
        if (isOpen)
        {
            UpdateUI();
        }
    }

    public void ChangeCategory(int index)
    {
        if (index < 0 || index >= tabs.Length)
            return;

        currentTab = index;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Title
        if (titleText != null && currentTab >= 0 && currentTab < tabs.Length)
        {
            titleText.text = tabs[currentTab];
        }

        Debug.Log("Updating UI with items for: " + tabs[currentTab]);

        if (shopManager == null || itemsParent == null || itemCardPrefab == null)
        {
            Debug.LogWarning("ShopUI is missing references (shopManager / itemsParent / itemCardPrefab).");
            return;
        }

        // Clear old item cards
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsParent.GetChild(i).gameObject);
        }

        // Map tab index to Type enum
        Type typeForTab = GetTypeForTab(currentTab);

        // Get items for this category
        ShopItemSO[] items = shopManager.GetItemsByType(typeForTab);

        // Create cards
        for (int i = 0; i < items.Length; i++)
        {
            ShopItemSO item = items[i];
            if (item == null)
                continue;

            ShopItemCard card = Instantiate(itemCardPrefab, itemsParent);
            card.Setup(item);
            // If your ShopItemCard has a reference back to the shop:
            // card.Setup(item, this);
        }
    }

    private Type GetTypeForTab(int tabIndex)
    {
        // Assumes your Type enum is in the same order as the tabs.
        // If not, you can map manually with a switch.
        Type[] typeValues = (Type[])System.Enum.GetValues(typeof(Type));

        if (tabIndex >= 0 && tabIndex < typeValues.Length)
        {
            return typeValues[tabIndex];
        }

        // Fallback
        return Type.Seating;
    }
}