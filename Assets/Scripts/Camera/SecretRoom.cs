using System.Collections;
using UnityEngine;
using Cinemachine;

// One volume owns both halves: the camera and the cover. Split across two triggers, a player
// who leaves by a route the second one does not cover stays black or stays on the room camera.
[RequireComponent(typeof(Collider2D))]
public class SecretRoom : MonoBehaviour {
    [Header("Camera")]
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private CinemachineVirtualCamera roomCam;

    [Header("Cover")]
    [SerializeField] private SpriteRenderer cover;
    [SerializeField] private float fadeTime = 0.35f;

    // The player carries several colliders; one of them leaving is not the player leaving.
    private int inside;
    private Coroutine fade;

    private void Awake() {
        GetComponent<Collider2D>().isTrigger = true;

        if (cameraSwitcher == null)
            Debug.LogError($"[SecretRoom] '{name}' has no Camera Switcher assigned.", this);

        if (roomCam == null)
            Debug.LogError($"[SecretRoom] '{name}' has no Room Cam assigned.", this);

        SetCoverAlpha(1f);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;

        inside++;
        if (inside > 1) return;

        if (cameraSwitcher != null && roomCam != null) cameraSwitcher.SwitchTo(roomCam);
        Fade(0f);
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;

        inside--;
        if (inside > 0) return;

        inside = 0;
        Leave();
    }

    // Dying, resting or a scene change inside the room must not strand the room camera.
    private void OnDisable() {
        if (inside <= 0) return;

        inside = 0;
        if (cameraSwitcher != null) cameraSwitcher.SwitchToGameplayCam();
        SetCoverAlpha(1f);
    }

    private void Leave() {
        if (cameraSwitcher != null) cameraSwitcher.SwitchToGameplayCam();
        Fade(1f);
    }

    private void Fade(float target) {
        if (cover == null) return;

        // Deactivating the volume makes Unity fire OnTriggerExit2D on the way out, and a
        // coroutine cannot start on an object that is already going away.
        if (!isActiveAndEnabled) {
            SetCoverAlpha(target);
            return;
        }

        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeTo(target));
    }

    // Unscaled: a card or panel opening on the way in would otherwise freeze it half faded.
    private IEnumerator FadeTo(float target) {
        float from = cover.color.a;
        float t = 0f;

        while (t < fadeTime) {
            t += Time.unscaledDeltaTime;
            SetCoverAlpha(Mathf.Lerp(from, target, t / fadeTime));
            yield return null;
        }

        SetCoverAlpha(target);
        fade = null;
    }

    private void SetCoverAlpha(float a) {
        if (cover == null) return;

        Color c = cover.color;
        c.a = a;
        cover.color = c;
    }
}
