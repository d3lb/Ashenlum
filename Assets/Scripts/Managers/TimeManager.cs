using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    private static readonly HashSet<Object> holders = new HashSet<Object>();

    private Coroutine hitStopCoroutine;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Freeze stack ──────────────────────────────────────────────────────────
    public static void Freeze(Object owner)
    {
        if (owner == null) return;
        holders.Add(owner);
        Apply();
    }

    public static void Release(Object owner)
    {
        if (owner == null) return;
        holders.Remove(owner);
        Apply();
    }

    public static void ReleaseAll()
    {
        holders.Clear();
        Apply();
    }

    private static void Apply()
    {
        // Drop anything Unity has destroyed since it registered.
        holders.RemoveWhere(o => o == null);
        Time.timeScale = holders.Count > 0 ? 0f : 1f;
    }

    // ── Hitstop ───────────────────────────────────────────────────────────────
    public void HitStop(float duration)
    {
        // restart current hitstop
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }

        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Freeze(this);

        yield return new WaitForSecondsRealtime(duration);

        Release(this);

        hitStopCoroutine = null;
    }

    // An interrupted hitstop would otherwise hold the freeze forever.
    private void OnDisable()
    {
        hitStopCoroutine = null;
        Release(this);
    }
}
