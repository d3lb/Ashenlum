using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Resolved, so the row never sees a RunSave or an id.
public class SlotSummary
{
    public bool   used;
    public int    hp;
    public int    maxHp;
    public int    lumens;
    public int    deaths;
    public int    kills;
    public float  playTime;
    public string area;

    public Sprite   ability;
    public Sprite[] talismans;
}

public class SaveSlotUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text titleText;

    [Header("Stats")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text lumenText;

    [Header("Loadout")]
    [SerializeField] private Image   abilityIcon;
    [SerializeField] private Image[] talismanIcons;

    [Header("Run")]
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text areaText;

    [Header("States")]
    // The empty label replaces this on a free slot.
    [SerializeField] private GameObject filledGroup;
    [SerializeField] private GameObject emptyLabel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button eraseButton;

    private System.Action onPlay;
    private System.Action onErase;

    // Two rows sharing a button: only the last one bound responds.
    private static readonly Dictionary<Button, SaveSlotUI> claimed = new();

    private void Awake()
    {
        Claim(playButton,  "Play Button");
        Claim(eraseButton, "Erase Button");

        if (playButton != null)  playButton.onClick.AddListener(() => onPlay?.Invoke());
        if (eraseButton != null) eraseButton.onClick.AddListener(() => onErase?.Invoke());
    }

    private void OnDestroy()
    {
        if (playButton != null)  claimed.Remove(playButton);
        if (eraseButton != null) claimed.Remove(eraseButton);
    }

    private void Claim(Button button, string field)
    {
        if (button == null)
        {
            Debug.LogError($"[SaveSlotUI] '{name}' has no {field} assigned.", this);
            return;
        }

        if (claimed.TryGetValue(button, out SaveSlotUI owner) && owner != null && owner != this)
        {
            Debug.LogError(
                $"[SaveSlotUI] '{name}' and '{owner.name}' both use the same {field} " +
                $"('{button.name}'). Only one row will respond. Re-drag this row's own " +
                "button into its field.", this);
            return;
        }

        claimed[button] = this;
    }

    public void Bind(int profileId, SlotSummary summary, System.Action play, System.Action erase)
    {
        onPlay  = play;
        onErase = erase;

        if (titleText != null) titleText.text = $"{profileId + 1}.";

        bool used = summary != null && summary.used;

        if (filledGroup != null) filledGroup.SetActive(used);
        if (emptyLabel != null)  emptyLabel.SetActive(!used);

        // Play hides, erase greys, so rows keep their shape.
        if (playButton != null)  playButton.gameObject.SetActive(used);
        if (eraseButton != null) eraseButton.interactable = used;

        if (!used) return;

        if (hpText != null)       hpText.text = $"{summary.hp}/{summary.maxHp}";
        if (lumenText != null)    lumenText.text = summary.lumens.ToString();
        if (playTimeText != null) playTimeText.text = FormatTime(summary.playTime);
        if (deathsText != null)   deathsText.text = $"{summary.deaths} deaths";
        if (killsText != null)    killsText.text = $"{summary.kills} kills";
        if (areaText != null)     areaText.text = summary.area;

        SetIcon(abilityIcon, summary.ability);

        if (talismanIcons != null)
            for (int i = 0; i < talismanIcons.Length; i++)
                SetIcon(talismanIcons[i],
                        summary.talismans != null && i < summary.talismans.Length
                            ? summary.talismans[i] : null);
    }

    // Hidden rather than a blank white box.
    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static string FormatTime(float seconds)
    {
        int total   = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours   = total / 3600;
        int minutes = (total % 3600) / 60;

        return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
    }
}
