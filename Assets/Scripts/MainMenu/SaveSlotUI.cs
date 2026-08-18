using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Button   playButton;
    [SerializeField] private Button   eraseButton;

    private System.Action onPlay;
    private System.Action onErase;

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

    public void Bind(int profileId, ProfileEntry entry, bool used,
                     System.Action play, System.Action erase)
    {
        onPlay  = play;
        onErase = erase;

        if (titleText != null) titleText.text = $"{profileId + 1}.";

        if (detailText != null)
        {
            if (!used)              detailText.text = "Empty";
            else if (entry == null) detailText.text = "Saved game";   // file with no index entry
            else                    detailText.text = $"{FormatTime(entry.playTime)}   ·   {entry.deaths} deaths";
        }

        if (playButton != null)  playButton.gameObject.SetActive(used);
        if (eraseButton != null) eraseButton.interactable = used;
    }

    private static string FormatTime(float seconds)
    {
        int total   = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hours   = total / 3600;
        int minutes = (total % 3600) / 60;

        return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
    }
}
