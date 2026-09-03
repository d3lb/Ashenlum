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

    // Scene names are not player-facing. "Right" means nothing; "Shattered Grove" does.
    [SerializeField] private AreaName[] areaNames;

    [System.Serializable]
    public class AreaName
    {
        public string scene;
        public string display;
    }

    private string DisplayNameFor(string scene)
    {
        if (string.IsNullOrEmpty(scene)) return "";

        if (areaNames != null)
            foreach (AreaName area in areaNames)
                if (area != null && area.scene == scene) return area.display;

        return scene;
    }

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
            slots[i].Bind(id, Summarise(id, index.Get(id)), () => Play(id), () => Erase(id));
        }
    }

    // Resolves ids to sprites here so the row never sees a save format.
    private SlotSummary Summarise(int profileId, ProfileEntry entry)
    {
        var summary = new SlotSummary { used = SaveSystem.HasRun(profileId) };
        if (!summary.used) return summary;

        if (entry != null)
        {
            summary.playTime = entry.playTime;
            summary.deaths   = entry.deaths;
            summary.kills    = entry.kills;
        }

        RunSave save = SaveSystem.LoadRun(profileId);
        if (save == null) return summary;

        summary.hp     = save.currentHp;
        summary.maxHp  = save.maxHp;
        summary.lumens = save.lumens;
        summary.area   = DisplayNameFor(save.currentArea);

        GameAssetDatabase db = GameManager.Instance != null ? GameManager.Instance.Assets : null;
        if (db == null) return summary;

        summary.ability = db.FindAbility(save.equippedAbility)?.icon;

        summary.talismans = new Sprite[save.equippedTalismans.Count];
        for (int i = 0; i < save.equippedTalismans.Count; i++)
            summary.talismans[i] = db.FindGood<Talisman>(save.equippedTalismans[i])?.icon;

        return summary;
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
