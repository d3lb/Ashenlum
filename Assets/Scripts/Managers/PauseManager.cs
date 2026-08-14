using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    private LumenUI lumenUI;
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
        TimeManager.Release(this);
    }

    public void RegisterLumenUI(LumenUI ui)
    {
        lumenUI = ui;
    }

    private void Update()
    {
        if (GameManager.Instance.activeRun.isTransitioningScenes)
            return;

        // The inventory owns Escape while it's open (it closes itself), so the
        // pause menu must not also pop up underneath it.
        if (InventoryUI.IsOpen || ShopUI.IsOpen)
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

        lumenUI?.Show();

        TimeManager.Freeze(this);
    }

    public void Resume()
    {
        paused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        lumenUI?.Hide();
        TimeManager.Release(this);
    }

    public void ExitToMenu()
    {
        TimeManager.ReleaseAll();
        GameManager.Instance.GoToScene("MainMenu", "");
    }
}