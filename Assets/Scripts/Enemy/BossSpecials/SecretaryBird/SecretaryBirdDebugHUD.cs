using UnityEngine;

/// <summary>
/// Drop this on the boss while tuning. It answers "what is he doing right now" without
/// making you read the Console. Delete or disable it for a build.
///
/// Reading it: "Wall Dash (low)" crossing the arena and missing you is the honest dash
/// doing its job. "Fake Dash" is the one that flashes low then goes over your head.
/// If those two look the same on screen, that is a tuning problem, not a naming one -
/// widen the gap between feintHeight and perchHeight.
/// </summary>
public class SecretaryBirdDebugHUD : MonoBehaviour
{
    [SerializeField] private SecretaryBirdBrain brain;
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdHealth health;

    [Header("Display")]
    [SerializeField] private bool show = true;
    [SerializeField] private Vector2 screenPos = new Vector2(12f, 12f);
    [SerializeField] private int fontSize = 16;
    [Tooltip("Also floats the attack name above the boss in the Scene/Game view.")]
    [SerializeField] private bool showWorldLabel = true;

    private GUIStyle style;
    private string lastName = "-";
    private float lastChange;

    private void Awake()
    {
        if (brain == null)  brain  = GetComponent<SecretaryBirdBrain>();
        if (state == null)  state  = GetComponent<SecretaryBirdState>();
        if (health == null) health = GetComponent<SecretaryBirdHealth>();
    }

    private void Update()
    {
        string n = brain != null && brain.CurrentAttack != null
            ? brain.CurrentAttack.DisplayName
            : "-";

        if (n != lastName)
        {
            lastName = n;
            lastChange = Time.time;
        }
    }

    private void OnGUI()
    {
        if (!show || state == null) return;

        if (style == null)
            style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, fontStyle = FontStyle.Bold };

        string hp = health != null ? $"{health.CurrentHP}/{health.MaxHP}" : "?";

        string text =
            $"SECRETARY BIRD\n" +
            $"attack : {lastName}  ({Time.time - lastChange:0.0}s)\n" +
            $"state  : {state.CurrentState}\n" +
            $"phase  : {state.Phase}\n" +
            $"hp     : {hp}\n" +
            $"facing : {(state.IsFacingRight ? "right" : "left")}";

        var rect = new Rect(screenPos.x, screenPos.y, 380f, 160f);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);

        style.normal.textColor = state.CurrentState == SecretaryBirdState.BossStateType.Recover
            ? Color.green
            : state.CurrentState == SecretaryBirdState.BossStateType.Attacking
                ? Color.red
                : Color.white;
        GUI.Label(rect, text, style);

        if (!showWorldLabel || Camera.main == null) return;

        Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (sp.z <= 0f) return;

        var wrect = new Rect(sp.x - 100f, Screen.height - sp.y - 20f, 200f, 24f);
        var wstyle = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(wrect, lastName, wstyle);
    }
}
