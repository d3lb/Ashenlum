using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    private GameObject pausePanel;
    private bool paused;

    public bool IsPaused => paused;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterMenu(GameObject menu)
    {
        pausePanel = menu;
        pausePanel.SetActive(false);
        paused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (GameManager.Instance.activeRun.isTransitioningScenes)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (paused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        if (pausePanel == null)
            return;

        paused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        paused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        GameManager.Instance.GoToScene("MainMenu", "");
    }
}