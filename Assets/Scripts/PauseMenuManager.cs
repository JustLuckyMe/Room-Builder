using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject settingsMenuUI;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);

        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(false);

        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);

        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void OpenSettings()
    {
        if (settingsMenuUI == null)
            return;

        settingsMenuUI.SetActive(true);
        pauseMenuUI.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsMenuUI == null)
            return;

        settingsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}