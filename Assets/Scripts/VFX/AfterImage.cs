using System.Collections.Generic;
using UnityEngine;

// Drops fading copies of a sprite while it is playing. Dense enough, the copies overlap into
// a blur; sparse enough, they read as a trail. Owner agnostic: the player's dash can use it too.
public class AfterImage : MonoBehaviour {
    [SerializeField] private SpriteRenderer source;

    [Header("Timing")]
    // Without it the first ghost lands on the spot he launched from, before he has moved.
    [SerializeField] private float startDelay = 0.05f;
    [SerializeField] private float interval = 0.03f;
    [SerializeField] private float life = 0.18f;

    // A near black sprite copied as is vanishes against a dark scene, so the ghosts are tinted.
    [Header("Look")]
    [SerializeField] private Color tint = new Color(1f, 0.45f, 0.2f, 0.5f);
    [SerializeField] private int sortingOffset = -1;

    private readonly List<SpriteRenderer> ghosts = new();
    private readonly List<float> born = new();

    private bool playing;
    private float nextSpawn;

    private void Awake() {
        if (source == null) source = GetComponent<SpriteRenderer>();

        if (source == null)
            Debug.LogError($"[AfterImage] '{name}' has no Source, so it will never spawn.", this);
    }

    public void Play() {
        playing = true;
        nextSpawn = Time.time + startDelay;
    }

    // Only stops spawning. The ghosts already out keep fading on their own.
    public void Stop() => playing = false;

    private void Update() {
        if (playing && Time.time >= nextSpawn) {
            Spawn();
            nextSpawn = Time.time + interval;
        }

        Fade();
    }

    // Unparented, so a ghost stays where it was born instead of riding the owner forward.
    private void Spawn() {
        if (source == null || source.sprite == null) return;

        GameObject go = new GameObject("AfterImage");
        go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;

        SpriteRenderer ghost = go.AddComponent<SpriteRenderer>();
        ghost.sprite = source.sprite;
        ghost.flipX = source.flipX;
        ghost.flipY = source.flipY;
        ghost.sortingLayerID = source.sortingLayerID;
        ghost.sortingOrder = source.sortingOrder + sortingOffset;
        ghost.color = tint;

        ghosts.Add(ghost);
        born.Add(Time.time);
    }

    // Walked backwards so removing an entry cannot skip the next one.
    private void Fade() {
        for (int i = ghosts.Count - 1; i >= 0; i--) {
            float age = (Time.time - born[i]) / life;

            if (ghosts[i] == null || age >= 1f) {
                if (ghosts[i] != null) Destroy(ghosts[i].gameObject);

                ghosts.RemoveAt(i);
                born.RemoveAt(i);
                continue;
            }

            Color c = tint;
            c.a = tint.a * (1f - age);
            ghosts[i].color = c;
        }
    }

    // The ghosts are not children, so a dying owner would leave them on the ground forever.
    private void OnDisable() {
        playing = false;

        for (int i = 0; i < ghosts.Count; i++)
            if (ghosts[i] != null) Destroy(ghosts[i].gameObject);

        ghosts.Clear();
        born.Clear();
    }
}
