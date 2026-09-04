using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Camera exposure, not a black overlay. The Volume is per-scene; the setting is not.
public class BrightnessApplier : MonoBehaviour
{
    private ColorAdjustments colorAdjustments;

    // Not GameManager.OnSceneReady - both are on Managers and OnEnable order is undefined.
    private void OnEnable()
    {
        GameSettings.OnBrightnessChanged += Apply;
        SceneManager.sceneLoaded += OnSceneLoaded;

        Find();
    }

    private void OnDisable()
    {
        GameSettings.OnBrightnessChanged -= Apply;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Find();

    private void Find()
    {
        colorAdjustments = null;

        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (v.profile == null || !v.isGlobal) continue;
            if (v.profile.TryGet(out colorAdjustments)) break;
        }

        // MainMenu has no Volume.
        if (colorAdjustments == null) return;

        Apply();
    }

    private void Apply()
    {
        if (colorAdjustments == null) return;

        // Ignored unless overrideState is set.
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = GameSettings.BrightnessExposure;
    }
}
