using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// SettingsPanel.Open(() => menuPanel.SetActive(true));
public class SettingsPanel : MonoBehaviour {
    public static SettingsPanel Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [SerializeField] private Button backButton;

    [Header("Audio")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown windowDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private Slider brightnessSlider;

    [Header("Game")]
    [SerializeField] private Slider shakeSlider;

    [Header("Value labels")]
    [SerializeField] private TMP_Text masterLabel;
    [SerializeField] private TMP_Text musicLabel;
    [SerializeField] private TMP_Text sfxLabel;
    [SerializeField] private TMP_Text brightnessLabel;
    [SerializeField] private TMP_Text shakeLabel;

    private readonly List<Vector2Int> resolutions = new();

    private System.Action onClose;

    public static bool IsOpen { get; private set; }

    // Blocks listeners while Refresh writes loaded values into the controls.
    private bool building;
    private bool built;

    // Own panel, not the static flag - another scene's instance may have left it set.
    private void Update() {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    // Unconditional, or a teardown while open leaves UIState.Busy stuck true.
    private void OnDisable() {
        IsOpen = false;
    }

    // An inactive GameObject never runs Awake, so setup cannot live only there.
    private void EnsureBuilt() {
        if (built) return;
        built = true;

        if (panel == null) panel = gameObject;

        GameSettings.Load();

        BuildResolutions();
        BuildWindowModes();
        Bind();
    }

    private void Awake() {
        if (Instance != null && Instance != this)
            Debug.LogWarning($"[SettingsPanel] '{name}' is a second SettingsPanel in this " +
                             $"scene, alongside '{Instance.name}'. Only one will be used.", this);

        Instance = this;

        EnsureBuilt();
        panel.SetActive(false);
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    private void Bind() {
        Slide(masterSlider, masterLabel, GameSettings.SetMasterVolume, Percent);
        Slide(musicSlider, musicLabel, GameSettings.SetMusicVolume, Percent);
        Slide(sfxSlider, sfxLabel, GameSettings.SetSfxVolume, Percent);
        Slide(shakeSlider, shakeLabel, GameSettings.SetScreenShake, Percent);

        // Middle of the bar is normal, not half.
        Slide(brightnessSlider, brightnessLabel, GameSettings.SetBrightness, BrightnessPercent);

        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(v => { if (!building) GameSettings.SetVSync(v); });

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(i => {
                if (building || i < 0 || i >= resolutions.Count) return;
                GameSettings.SetResolution(resolutions[i].x, resolutions[i].y);
            });

        if (windowDropdown != null)
            windowDropdown.onValueChanged.AddListener(i => {
                if (building) return;
                GameSettings.SetWindowMode((GameSettings.WindowMode)i);
            });

        if (backButton != null) backButton.onClick.AddListener(Close);
    }

    private static string Percent(float v) => $"{Mathf.RoundToInt(v * 100f)}%";

    // 0 to 1 as 50-150%, so the middle reads as normal.
    private static string BrightnessPercent(float v) => $"{Mathf.RoundToInt(50f + v * 100f)}%";

    // Label updates while building too, so readouts are right on open.
    private void Slide(Slider slider, TMP_Text label, System.Action<float> set,
                       System.Func<float, string> format) {
        if (slider == null) return;

        slider.onValueChanged.AddListener(v => {
            if (label != null) label.text = format(v);
            if (!building) set(v);
        });
    }

    private void BuildWindowModes() {
        if (windowDropdown == null) return;

        windowDropdown.ClearOptions();
        windowDropdown.AddOptions(new List<string> { "Fullscreen", "Borderless", "Windowed" });
    }

    // Screen.resolutions repeats each size once per refresh rate.
    private void BuildResolutions() {
        resolutions.Clear();

        foreach (Resolution r in Screen.resolutions) {
            Vector2Int size = new Vector2Int(r.width, r.height);
            if (!resolutions.Contains(size)) resolutions.Add(size);
        }

        resolutions.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

        if (resolutionDropdown == null) return;

        List<string> labels = new();
        foreach (Vector2Int r in resolutions) labels.Add($"{r.x} x {r.y}");

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
    }

    public static void Open(System.Action whenClosed = null) {
        if (Instance == null) {
            Debug.LogError("[SettingsPanel] No SettingsPanel in the scene, so the " +
                           "request was dropped. It must sit on an object that is " +
                           "active, or Awake never runs and this stays null.");
            whenClosed?.Invoke();
            return;
        }

        Instance.Show(whenClosed);
    }

    private void Show(System.Action whenClosed) {
        onClose = whenClosed;

        EnsureBuilt();
        Refresh();

        panel.SetActive(true);
        IsOpen = true;
    }

    public void Close() {
        EnsureBuilt();

        IsOpen = false;
        panel.SetActive(false);

        // Cleared first so the callback may open something else.
        System.Action callback = onClose;
        onClose = null;
        callback?.Invoke();
    }

    // Saved values, not whatever the prefab was authored with.
    private void Refresh() {
        building = true;

        Set(masterSlider, masterLabel, GameSettings.MasterVolume, Percent);
        Set(musicSlider, musicLabel, GameSettings.MusicVolume, Percent);
        Set(sfxSlider, sfxLabel, GameSettings.SfxVolume, Percent);
        Set(shakeSlider, shakeLabel, GameSettings.ScreenShake, Percent);
        Set(brightnessSlider, brightnessLabel, GameSettings.Brightness, BrightnessPercent);

        if (vsyncToggle != null) vsyncToggle.isOn = GameSettings.VSync;

        if (windowDropdown != null) {
            windowDropdown.SetValueWithoutNotify((int)GameSettings.Window);
            windowDropdown.RefreshShownValue();
        }

        if (resolutionDropdown != null) {
            int index = resolutions.IndexOf(new Vector2Int(GameSettings.Width, GameSettings.Height));

            // Saved size this monitor cannot do falls back to the largest.
            if (index < 0) index = resolutions.Count - 1;

            resolutionDropdown.SetValueWithoutNotify(index);
            resolutionDropdown.RefreshShownValue();
        }

        building = false;
    }

    private static void Set(Slider slider, TMP_Text label, float value,
                            System.Func<float, string> format) {
        if (slider != null) slider.value = value;
        if (label != null) label.text = format(value);
    }
}
