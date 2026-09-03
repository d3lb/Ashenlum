using UnityEngine;

// Temporary tuning window for the King fight. Put it on him and press F2.
// Reads only - nothing here changes the fight.
public class KingDebugHUD : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private bool openOnStart = true;

    [SerializeField] private KingBrain brain;
    [SerializeField] private KingState state;
    [SerializeField] private KingHealth health;

    private bool open;
    private Rect window = new Rect(Screen.width - 340, 20, 320, 460);

    private void Awake()
    {
        if (brain == null) brain = GetComponent<KingBrain>();
        if (state == null) state = GetComponent<KingState>();
        if (health == null) health = GetComponent<KingHealth>();

        open = openOnStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) open = !open;
    }

    private void OnGUI()
    {
        if (!open || brain == null) return;

        window = GUI.Window(4821, window, Draw, "King  (F2)");
    }

    private void Draw(int id)
    {
        GUILayout.Label($"State:  {state.CurrentState}");
        GUILayout.Label($"Phase:  {state.Phase}   ({brain.PaceNow.name})");

        if (health != null)
        {
            Bar($"HP  {health.CurrentHP}/{health.MaxHP}", health.Normalized);

            // Jumping to just above a threshold rather than onto it, so the next hit
            // is what crosses it and you see the transition fire properly.
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("full")) SetPercent(1f);
            if (GUILayout.Button("ph2")) SetPercent(0.65f);
            if (GUILayout.Button("ph3")) SetPercent(0.32f);
            if (GUILayout.Button("1 hp")) health.DebugSetHealth(1);
            if (GUILayout.Button("kill")) health.DebugSetHealth(0);
            GUILayout.EndHorizontal();
        }

        Section("Doing now");

        if (!brain.Active)
        {
            GUILayout.Label("  not active");
        }
        else
        {
            GUILayout.Label($"  main:  {Name(brain.CurrentMain)}");
            GUILayout.Label($"  extra: {Name(brain.CurrentExtra)}");
        }

        Section("Greed  (your stability)");

        // The whole point of the fight, so it is the number worth watching.
        GUILayout.Label($"  timing scale: {brain.GreedNow:0.00}x");
        GUILayout.Label($"  idle {brain.PaceNow.idleBeat * brain.GreedNow:0.00}s   " +
                        $"recovery x{brain.PaceNow.recoveryScale * brain.GreedNow:0.00}");

        Section("Punish");
        GUILayout.Label($"  hits {brain.PunishProgress}/{brain.PunishHitsNeeded}" +
                        (brain.PunishAttack == null ? "   (no attack assigned)" : ""));

        Section($"Draw odds  (phase {state.Phase})");
        DrawOdds();

        Section("Scripted");
        GUILayout.Label($"  transition: {Name(brain.TransitionAttack)}");
        GUILayout.Label($"  punish:     {Name(brain.PunishAttack)}");

        GUI.DragWindow();
    }

    private void DrawOdds()
    {
        if (brain.Pool == null) return;

        int total = 0;
        foreach (KingAttack a in brain.Pool)
            if (a != null && a.CanUse(state.Phase)) total += a.Weight;

        if (total == 0)
        {
            GUILayout.Label("  nothing usable in this phase");
            return;
        }

        foreach (KingAttack a in brain.Pool)
        {
            if (a == null) continue;

            bool usable = a.CanUse(state.Phase);
            string tag = a.Scripted ? "scripted" : usable ? $"{100f * a.Weight / total:0}%" : "-";

            GUILayout.Label($"  {a.DisplayName,-22} {tag}");
        }

        // The bag draws without replacement and never repeats the last move, so the
        // real odds drift from these. Close enough for tuning weights.
        GUILayout.Label("  (weight share, not exact)");
    }

    private void SetPercent(float percent) =>
        health.DebugSetHealth(Mathf.RoundToInt(health.MaxHP * Mathf.Clamp01(percent)));

    private static string Name(KingAttack a) => a != null ? a.DisplayName : "-";

    private static void Section(string label)
    {
        GUILayout.Space(6);
        GUILayout.Label($"-- {label} --");
    }

    private static void Bar(string label, float fill)
    {
        GUILayout.Label(label);

        Rect r = GUILayoutUtility.GetRect(100, 12);
        GUI.Box(r, GUIContent.none);

        r.width *= Mathf.Clamp01(fill);
        GUI.color = Color.Lerp(Color.red, Color.white, fill);
        GUI.Box(r, GUIContent.none);
        GUI.color = Color.white;
    }
}
