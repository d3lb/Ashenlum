using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string firstScene;

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject menuPanel;


    public void Start()
    {
        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void StartGame()
    {
        GameManager.Instance.StartNewGame();
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void EnterSettings()
    {
        if (settingsPanel == null)
            return;

        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ExitSettings()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }
}