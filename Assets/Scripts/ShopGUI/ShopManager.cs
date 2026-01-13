using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] private ShopItemSO[] allItems;

    [Header("References")]
    [SerializeField] private PlayerBalance balance;

    // Simple buy by index (for UI buttons that know their index)
    public void BuyItem(int index)
    {
        if (index < 0 || index >= allItems.Length)
            return;

        ShopItemSO item = allItems[index];
        TryBuy(item);
    }

    public bool TryBuy(ShopItemSO item)
    {
        if (item == null)
            return false;

        if (!balance.TrySpend(item.price))
        {
            Debug.Log("Not enough money to buy: " + item.itemName);
            return false;
        }

        Debug.Log("Bought item: " + item.itemName);

        // Later: hand off to placement system
        // PlacementController.Instance.StartPlacement(item.prefab, item.data);

        return true;
    }

    public ShopItemSO[] GetItems()
    {
        return allItems;
    }

    public ShopItemSO[] GetItemsByType(Type type)
    {
        List<ShopItemSO> result = new List<ShopItemSO>();

        for (int i = 0; i < allItems.Length; i++)
        {
            ShopItemSO item = allItems[i];
            if (item == null || item.data == null)
                continue;

            if (item.data.ObjectType == type)
            {
                result.Add(item);
            }
        }

        return result.ToArray();
    }

    public ShopItemSO[] GetItemsByStyle(StyleType style)
    {
        List<ShopItemSO> result = new List<ShopItemSO>();

        for (int i = 0; i < allItems.Length; i++)
        {
            ShopItemSO item = allItems[i];
            if (item == null || item.data == null)
                continue;

            if (item.data.Style == style)
            {
                result.Add(item);
            }
        }

        return result.ToArray();
    }

    public ShopItemSO[] GetItemsByColorType(ColorType colorType)
    {
        List<ShopItemSO> result = new List<ShopItemSO>();

        for (int i = 0; i < allItems.Length; i++)
        {
            ShopItemSO item = allItems[i];
            if (item == null || item.data == null)
                continue;

            if (item.data.colorType == colorType)
            {
                result.Add(item);
            }
        }

        return result.ToArray();
    }
}