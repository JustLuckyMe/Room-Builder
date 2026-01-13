using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTitle : MonoBehaviour
{
    int GameTitleScene = 0;
    int GameScene = 1;
    [SerializeField] GameObject LevelSelection;

    private void Start()
    {
        if (LevelSelection != null) LevelSelection.SetActive(false);
    }

    public void ChangeScene(int scene)
    {
        SceneManager.LoadSceneAsync(scene);
    }

    public void OpenLevelSelection()
    {
        LevelSelection.SetActive(!LevelSelection.activeSelf);
    }
}
