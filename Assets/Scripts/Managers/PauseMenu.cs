using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private void Awake()
    {
        PauseManager.Instance.RegisterMenu(pausePanel);
    }

    public void Resume()
    {
        PauseManager.Instance.Resume();
    }

    public void QuitToMenu()
    {
        PauseManager.Instance.ExitToMenu();
    }
}