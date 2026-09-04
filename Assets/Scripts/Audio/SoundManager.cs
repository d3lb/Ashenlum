using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Static Play, so a missing manager is silence and no caller needs a reference.
public class SoundManager : MonoBehaviour {
    public static SoundManager Instance { get; private set; }

    [Header("Bank")]
    [SerializeField] private SoundBank bank;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Voices")]
    // Pool, not PlayOneShot: sounds need stopping and independent pitch.
    [SerializeField] private int voices = 8;

    [Header("Music")]
    [SerializeField] private float musicCrossfade = 1.5f;

    // Must match the mixer's exposed names exactly.
    [Header("Mixer parameters")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string sfxParam = "SFXVolume";
    [SerializeField] private string musicParam = "MusicVolume";

    private AudioSource[] pool;
    private int next;

    private AudioSource musicA;
    private AudioSource musicB;
    private bool musicOnA;
    private Coroutine musicRoutine;

    private readonly Dictionary<SoundId, float> nextAllowed = new();

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        pool = new AudioSource[Mathf.Max(1, voices)];
        for (int i = 0; i < pool.Length; i++) pool[i] = MakeSource($"SFX {i}", sfxGroup);

        musicA = MakeSource("Music A", musicGroup);
        musicB = MakeSource("Music B", musicGroup);
        musicA.loop = musicB.loop = true;

        // Pulled, so load order with GameSettings cannot matter.
        GameSettings.Load();
        GameSettings.ApplyAudio();
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    private AudioSource MakeSource(string name, AudioMixerGroup group) {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.outputAudioMixerGroup = group;

        // 2D: the camera follows the player.
        s.spatialBlend = 0f;

        return s;
    }

    // ── one-shots ────────────────────────────────────────────────────────────────

    public static void Play(SoundId id) => Instance?.PlayInternal(id, null);

    public static void PlayAt(SoundId id, Vector3 position) =>
        Instance?.PlayInternal(id, position);

    private void PlayInternal(SoundId id, Vector3? position) {
        if (bank == null) return;

        SoundBank.Entry entry = bank.Find(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0) return;

        // Unscaled, so a sound asked for while frozen still belongs to now.
        if (nextAllowed.TryGetValue(id, out float allowed) && Time.unscaledTime < allowed)
            return;

        nextAllowed[id] = Time.unscaledTime + entry.minInterval;

        AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return;

        AudioSource s = Take();

        s.clip = clip;
        s.volume = entry.volume;
        s.pitch = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);

        if (position.HasValue) {
            s.transform.position = position.Value;
            s.spatialBlend = 1f;
        }
        else {
            s.transform.localPosition = Vector3.zero;
            s.spatialBlend = 0f;
        }

        s.Play();
    }

    // Steals the oldest only when every voice is busy.
    private AudioSource Take() {
        for (int i = 0; i < pool.Length; i++) {
            AudioSource s = pool[(next + i) % pool.Length];
            if (!s.isPlaying) {
                next = (next + i + 1) % pool.Length;
                return s;
            }
        }

        AudioSource oldest = pool[next];
        next = (next + 1) % pool.Length;
        return oldest;
    }

    // ── music ────────────────────────────────────────────────────────────────────

    // Same clip keeps playing, so room changes do not restart the track.
    public void PlayMusic(AudioClip clip, float fade = -1f) {
        AudioSource current = musicOnA ? musicA : musicB;
        if (current.clip == clip && current.isPlaying) return;

        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(Crossfade(clip, fade < 0f ? musicCrossfade : fade));
    }

    public void StopMusic(float fade = -1f) => PlayMusic(null, fade);

    private IEnumerator Crossfade(AudioClip clip, float duration) {
        AudioSource from = musicOnA ? musicA : musicB;
        AudioSource to = musicOnA ? musicB : musicA;
        musicOnA = !musicOnA;

        to.clip = clip;
        to.volume = 0f;
        if (clip != null) to.Play();

        float startFrom = from.volume;
        float t = 0f;

        while (t < duration) {
            t += Time.unscaledDeltaTime;
            float k = duration <= 0f ? 1f : t / duration;

            from.volume = Mathf.Lerp(startFrom, 0f, k);
            to.volume = clip != null ? Mathf.Lerp(0f, 1f, k) : 0f;
            yield return null;
        }

        from.Stop();
        from.clip = null;
        from.volume = 0f;
        to.volume = clip != null ? 1f : 0f;

        musicRoutine = null;
    }

    // ── volume, for the settings menu ────────────────────────────────────────────

    public void SetMasterVolume(float v) => SetVolume(masterParam, v);
    public void SetSfxVolume(float v) => SetVolume(sfxParam, v);
    public void SetMusicVolume(float v) => SetVolume(musicParam, v);

    // Mixer volume is logarithmic dB; a raw 0-1 slider wastes half its travel.
    private void SetVolume(string param, float linear) {
        if (mixer == null || string.IsNullOrEmpty(param)) return;

        float db = linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;

        if (!mixer.SetFloat(param, db))
            Debug.LogWarning($"[SoundManager] Mixer has no exposed parameter '{param}'.", this);
    }

    public float GetVolume(string param) {
        if (mixer == null || !mixer.GetFloat(param, out float db)) return 1f;
        return db <= -80f ? 0f : Mathf.Pow(10f, db / 20f);
    }
}
