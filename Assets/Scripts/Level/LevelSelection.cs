using UnityEngine;

public class LevelSelection : MonoBehaviour
{
    [SerializeField] LevelManager levelManager;
    [SerializeField] Transform levelSpawnPoint;
    [SerializeField] GameObject levelPrefab;

    private void Start()
    {
        SpawnLevels();
    }

    private void SpawnLevels()
    {
        if (levelManager == null || levelManager.Levels == null)
            return;

        // Clear old spawned levels
        foreach (Transform child in levelSpawnPoint)
            Destroy(child.gameObject);

        // Spawn one prefab per LevelSO
        for (int i = 0; i < levelManager.Levels.Length; i++)
        {
            LevelSO level = levelManager.Levels[i];
            if (level == null)
                continue;

            GameObject go = Instantiate(levelPrefab, levelSpawnPoint);

            // Optional: pass data into the prefab if it has a script for it
/*            LevelView view = go.GetComponent<LevelView>();
            if (view != null)
                view.SetLevel(level, i);*/
        }
    }
}