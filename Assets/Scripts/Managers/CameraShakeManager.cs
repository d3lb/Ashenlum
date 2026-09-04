using UnityEngine;
using Cinemachine;

public class CameraShakeManager : MonoBehaviour {
    public static CameraShakeManager Instance;

    private void Awake() {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    public void Shake(float duration, float amplitude, float frequency) {
        // Every shake comes through here, so the setting is one multiply.
        amplitude *= GameSettings.ScreenShake;
        if (amplitude <= 0.001f) return;

        var brain = CinemachineCore.Instance.GetActiveBrain(0);
        if (brain == null) return;

        var liveCam = brain.ActiveVirtualCamera as CinemachineVirtualCamera;
        if (liveCam == null) return;

        var shake = liveCam.GetComponent<CameraShake>();
        if (shake != null) shake.Shake(duration, amplitude, frequency);
    }
}