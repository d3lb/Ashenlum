using UnityEngine;

// Belongs to the machine, not the save profile - hence PlayerPrefs, not RunSave.
public static class GameSettings {
    private const string MasterKey = "vol_master";
    private const string MusicKey = "vol_music";
    private const string SfxKey = "vol_sfx";
    private const string WidthKey = "res_width";
    private const string HeightKey = "res_height";
    private const string WindowKey = "window_mode";
    private const string VSyncKey = "vsync";
    private const string BrightnessKey = "brightness";
    private const string ShakeKey = "screen_shake";

    // Three modes, so a bool will not do.
    public enum WindowMode { Fullscreen, Borderless, Windowed }

    public static float MasterVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    public static int Width { get; private set; }
    public static int Height { get; private set; }
    public static WindowMode Window { get; private set; } = WindowMode.Fullscreen;
    public static bool VSync { get; private set; } = true;

    // 0.5 is neutral.
    public static float Brightness { get; private set; } = 0.5f;

    public static float ScreenShake { get; private set; } = 1f;

    public static System.Action OnBrightnessChanged;

    private static bool loaded;

    public static void Load() {
        if (loaded) return;
        loaded = true;

        MasterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);

        // First run defaults to the launch resolution.
        Width = PlayerPrefs.GetInt(WidthKey, Screen.width);
        Height = PlayerPrefs.GetInt(HeightKey, Screen.height);
        Window = (WindowMode)PlayerPrefs.GetInt(WindowKey, (int)WindowMode.Fullscreen);

        VSync = PlayerPrefs.GetInt(VSyncKey, 1) == 1;
        Brightness = PlayerPrefs.GetFloat(BrightnessKey, 0.5f);
        ScreenShake = PlayerPrefs.GetFloat(ShakeKey, 1f);

        ApplyScreen();
        ApplyVSync();
    }

    public static void SetMasterVolume(float v) { MasterVolume = Clamp(v); ApplyAudio(); Write(); }
    public static void SetMusicVolume(float v) { MusicVolume = Clamp(v); ApplyAudio(); Write(); }
    public static void SetSfxVolume(float v) { SfxVolume = Clamp(v); ApplyAudio(); Write(); }

    public static void SetResolution(int width, int height) {
        Width = width;
        Height = height;

        ApplyScreen();
        Write();
    }

    public static void SetWindowMode(WindowMode mode) {
        Window = mode;

        ApplyScreen();
        Write();
    }

    public static void SetVSync(bool on) {
        VSync = on;

        ApplyVSync();
        Write();
    }

    public static void SetBrightness(float v) {
        Brightness = Clamp(v);

        // The Volume that applies it lives in the scene.
        OnBrightnessChanged?.Invoke();
        Write();
    }

    public static void SetScreenShake(float v) {
        ScreenShake = Clamp(v);
        Write();
    }

    // -1 to +1 EV around neutral.
    public static float BrightnessExposure => (Brightness - 0.5f) * 2f;

    // Pulled by SoundManager once its mixer exists, so load order cannot matter.
    public static void ApplyAudio() {
        SoundManager s = SoundManager.Instance;
        if (s == null) return;

        s.SetMasterVolume(MasterVolume);
        s.SetMusicVolume(MusicVolume);
        s.SetSfxVolume(SfxVolume);
    }

    private static FullScreenMode ModeOf(WindowMode m) => m switch {
        WindowMode.Fullscreen => FullScreenMode.ExclusiveFullScreen,
        WindowMode.Borderless => FullScreenMode.FullScreenWindow,
        _ => FullScreenMode.Windowed,
    };

    private static void ApplyScreen() {
        if (Width <= 0 || Height <= 0) return;

        FullScreenMode mode = ModeOf(Window);

        // SetResolution flickers the window even when the values already match.
        if (Screen.width == Width && Screen.height == Height && Screen.fullScreenMode == mode)
            return;

        Screen.SetResolution(Width, Height, mode);
    }

    private static void ApplyVSync() => QualitySettings.vSyncCount = VSync ? 1 : 0;

    private static void Write() {
        PlayerPrefs.SetFloat(MasterKey, MasterVolume);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);

        PlayerPrefs.SetInt(WidthKey, Width);
        PlayerPrefs.SetInt(HeightKey, Height);
        PlayerPrefs.SetInt(WindowKey, (int)Window);

        PlayerPrefs.SetInt(VSyncKey, VSync ? 1 : 0);
        PlayerPrefs.SetFloat(BrightnessKey, Brightness);
        PlayerPrefs.SetFloat(ShakeKey, ScreenShake);

        PlayerPrefs.Save();
    }

    private static float Clamp(float v) => Mathf.Clamp01(v);
}
