using UnityEngine;

public class PurchaseSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform centerSpawnPoint;
    [SerializeField] private Transform spawnedParent;
    [SerializeField] private bool useSpawnRotation = false;

    private void OnEnable()
    {
        ShopManager.OnItemPurchased += HandleItemPurchased;
    }

    private void OnDisable()
    {
        ShopManager.OnItemPurchased -= HandleItemPurchased;
    }

    private void HandleItemPurchased(ShopItemSO item)
    {
        if (item == null)
            return;

        if (centerSpawnPoint == null)
        {
            Debug.LogWarning("PurchaseSpawner: centerSpawnPoint is not assigned.");
            return;
        }

        if (item.prefab == null)
        {
            Debug.LogWarning("PurchaseSpawner: ShopItemSO prefab is null for item: " + item.itemName);
            return;
        }

        Vector3 pos = centerSpawnPoint.position;
        Quaternion rot = useSpawnRotation ? centerSpawnPoint.rotation : item.prefab.transform.rotation;

        GameObject spawned = Instantiate(item.prefab, pos, rot, spawnedParent);

        // Optional: name it nicely in the hierarchy
        spawned.name = item.itemName;
    }
}