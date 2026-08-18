using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject slotPanel;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button savesButton;

    [Header("Slots")]
    [SerializeField] private SaveSlotUI[] slots;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (slotPanel != null)     slotPanel.SetActive(false);
        if (menuPanel != null)     menuPanel.SetActive(true);

        SaveSystem.Compact();

        RefreshMenu();
    }

    private void RefreshMenu()
    {
        bool anySaves = SaveSystem.UsedCount() > 0;

        if (continueButton != null) continueButton.interactable = anySaves;
        if (savesButton != null)    savesButton.interactable = anySaves;
    }

    public void ContinueGame()
    {
        GameManager.Instance.ContinueGame();
    }

    public void NewGame()
    {
        if (GameManager.Instance.StartNewGame()) return;

        OpenSlots();
    }

    public void OpenSlots()
    {
        if (slotPanel == null) return;

        if (menuPanel != null) menuPanel.SetActive(false);
        slotPanel.SetActive(true);

        RefreshSlots();
    }

    public void CloseSlots()
    {
        if (slotPanel != null) slotPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);

        RefreshMenu();
    }

    private void RefreshSlots()
    {
        if (slots == null) return;

        ProfileIndex index = SaveSystem.LoadIndex();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            int id = i;

            slots[i].Bind(id, index.Get(id), SaveSystem.HasRun(id), () => Play(id), () => Erase(id));
        }
    }

    private void Play(int profileId)
    {
        if (SaveSystem.HasRun(profileId)) GameManager.Instance.LoadProfile(profileId);
    }

    private void Erase(int profileId)
    {
        ConfirmModal.Ask(
            $"Erase Slot {profileId + 1}?",
            "This save will be gone for good.",
            "Erase",
            () =>
            {
                GameManager.Instance.DeleteProfile(profileId);

                RefreshSlots();
                RefreshMenu();
            });
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void EnterSettings()
    {
        if (settingsPanel == null) return;

        if (menuPanel != null) menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ExitSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }
}
