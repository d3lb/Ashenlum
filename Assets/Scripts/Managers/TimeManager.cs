using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The single owner of Time.timeScale. Nothing else in the project should ever write it.
///
/// Four systems used to freeze time independently - pause, hitstop, PlayerMovement.Sleep
/// and the inventory - and every one of them ended by writing timeScale = 1f. So whichever
/// finished FIRST unfroze the game for everybody: get hit, open the inventory during the
/// ~50ms hitstop, and the hitstop's release resumes the world while the inventory is still
/// open and the player is still locked out.
///
/// Freezes are counted, not assigned. Time resumes only when the LAST holder lets go.
/// Hitstop is not special - it takes a hold like everyone else.
///
/// Freeze/Release are static so callers need no Instance and no null check, and they work
/// even before the Managers prefab has spawned.
/// </summary>
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
    public static bool IsFrozen => holders.Count > 0;
    public static int HolderCount => holders.Count;

    /// <summary>Hold time at zero. Calling twice with the same owner is harmless.</summary>
    public static void Freeze(Object owner)
    {
        if (owner == null) return;
        holders.Add(owner);
        Apply();
    }

    /// <summary>Let go. Time resumes only if nobody else is still holding.</summary>
    public static void Release(Object owner)
    {
        if (owner == null) return;
        holders.Remove(owner);
        Apply();
    }

    /// <summary>
    /// Hard reset. Call on scene transitions - a holder destroyed mid-freeze would
    /// otherwise keep the game stopped forever.
    /// </summary>
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

    // A hitstop interrupted by a scene change or a disable would otherwise hold the
    // freeze forever.
    private void OnDisable()
    {
        hitStopCoroutine = null;
        Release(this);
    }
}
