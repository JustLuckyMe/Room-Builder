using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Shop Item")]
public class ShopItemSO : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int price;
    public GameObject prefab;
    public ObjectDataSO data; // shared with prefab

    public Type ObjectType { get { return data != null ? data.ObjectType : default; } }
    public StyleType Style { get { return data != null ? data.Style : default; } }
    public ColorType ColorType { get { return data != null ? data.colorType : default; } }
}