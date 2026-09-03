using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Lives on Managers, next to GameManager and TimeManager. Survives scene loads, which
// music needs and one-shots do not care about.
//
// Callers use the static Play, so a missing SoundManager is silence rather than a crash
// and no script needs a reference to it.
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Bank")]
    [SerializeField] private SoundBank bank;

    [Header("Mixer")]
    // Routed through groups so the settings menu can move one slider per group.
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup musicGroup;

    [Header("Voices")]
    // A pool, not one shared source: PlayOneShot cannot be stopped or faded per sound,
    // and a pitch change on a shared source bends everything already playing.
    [SerializeField] private int voices = 8;

    [Header("Music")]
    [SerializeField] private float musicCrossfade = 1.5f;

    // Exposed parameter names on the mixer. Must match exactly or volume silently
    // does nothing, so they are fields rather than literals buried in code.
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        pool = new AudioSource[Mathf.Max(1, voices)];
        for (int i = 0; i < pool.Length; i++) pool[i] = MakeSource($"SFX {i}", sfxGroup);

        musicA = MakeSource("Music A", musicGroup);
        musicB = MakeSource("Music B", musicGroup);
        musicA.loop = musicB.loop = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private AudioSource MakeSource(string name, AudioMixerGroup group)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.outputAudioMixerGroup = group;

        // 2D by default. The camera follows the player, so panning player sounds by
        // world position only adds noise.
        s.spatialBlend = 0f;

        return s;
    }

    // ── one-shots ────────────────────────────────────────────────────────────────

    public static void Play(SoundId id) => Instance?.PlayInternal(id, null);

    // Positional, for things away from the player - a distant breakable, an enemy.
    public static void PlayAt(SoundId id, Vector3 position) =>
        Instance?.PlayInternal(id, position);

    private void PlayInternal(SoundId id, Vector3? position)
    {
        if (bank == null) return;

        SoundBank.Entry entry = bank.Find(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0) return;

        // Unscaled: a sound asked for while the game is frozen still belongs to now.
        if (nextAllowed.TryGetValue(id, out float allowed) && Time.unscaledTime < allowed)
            return;

        nextAllowed[id] = Time.unscaledTime + entry.minInterval;

        AudioClip clip = entry.clips[Random.Range(0, entry.clips.Length)];
        if (clip == null) return;

        AudioSource s = Take();

        s.clip = clip;
        s.volume = entry.volume;
        s.pitch = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);

        if (position.HasValue)
        {
            s.transform.position = position.Value;
            s.spatialBlend = 1f;
        }
        else
        {
            s.transform.localPosition = Vector3.zero;
            s.spatialBlend = 0f;
        }

        s.Play();
    }

    // Prefers a free voice; steals the oldest only when everything is busy.
    private AudioSource Take()
    {
        for (int i = 0; i < pool.Length; i++)
        {
            AudioSource s = pool[(next + i) % pool.Length];
            if (!s.isPlaying)
            {
                next = (next + i + 1) % pool.Length;
                return s;
            }
        }

        AudioSource oldest = pool[next];
        next = (next + 1) % pool.Length;
        return oldest;
    }

    // ── music ────────────────────────────────────────────────────────────────────

    // Same clip means keep playing. Walking between rooms in one area must not
    // restart the track.
    public void PlayMusic(AudioClip clip, float fade = -1f)
    {
        AudioSource current = musicOnA ? musicA : musicB;
        if (current.clip == clip && current.isPlaying) return;

        if (musicRoutine != null) StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(Crossfade(clip, fade < 0f ? musicCrossfade : fade));
    }

    public void StopMusic(float fade = -1f) => PlayMusic(null, fade);

    private IEnumerator Crossfade(AudioClip clip, float duration)
    {
        AudioSource from = musicOnA ? musicA : musicB;
        AudioSource to = musicOnA ? musicB : musicA;
        musicOnA = !musicOnA;

        to.clip = clip;
        to.volume = 0f;
        if (clip != null) to.Play();

        float startFrom = from.volume;
        float t = 0f;

        while (t < duration)
        {
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

    // Mixers work in decibels, which are logarithmic. Feeding a 0-1 slider straight in
    // makes the top half of its travel do almost nothing.
    private void SetVolume(string param, float linear)
    {
        if (mixer == null || string.IsNullOrEmpty(param)) return;

        float db = linear <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(linear)) * 20f;

        if (!mixer.SetFloat(param, db))
            Debug.LogWarning($"[SoundManager] Mixer has no exposed parameter '{param}'.", this);
    }

    public float GetVolume(string param)
    {
        if (mixer == null || !mixer.GetFloat(param, out float db)) return 1f;
        return db <= -80f ? 0f : Mathf.Pow(10f, db / 20f);
    }
}
