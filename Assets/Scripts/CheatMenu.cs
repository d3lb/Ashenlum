using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CheatMenu : MonoBehaviour {
    [Header("Toggle Key")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    // Drop every talisman, strength upgrade, bundle and ability asset in here.
    [Header("Grant")]
    [SerializeField] private ShopGood[] grantGoods;
    [SerializeField] private ActiveAbility[] grantAbilities;

    private bool isOpen = false;
    private Vector2 scrollPos;

    private Rect windowRect = new Rect(20, 20, 280, 500);
    private Camera cam;

    private void Update() {
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();


        if (Input.GetKeyDown(toggleKey))
            isOpen = !isOpen;

        if (isOpen && Input.GetMouseButtonDown(2)) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null) {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Mathf.Abs(cam.transform.position.z);

                Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);
                worldPos.z = player.transform.position.z;

                player.transform.position = worldPos;
            }
        }
    }

    private void OnGUI() {
        if (!isOpen) return;

        windowRect = GUI.Window(0, windowRect, DrawWindow, "Cheat Menu (F1)");
    }

    private void DrawWindow(int id) {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth ph = player?.GetComponent<PlayerHealth>();
        PlayerCombat pc = player?.GetComponent<PlayerCombat>();

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GameRunProfile run = GameManager.Instance?.activeRun;
        if (run == null) {
            GUILayout.Label("No active run found.");
            GUILayout.EndScrollView();
            GUI.DragWindow();
            return;
        }

        //  WORLD STATE 
        Section("World State");

        GUILayout.Label($"Area: {run.currentArea}");


        //  RESOURCES 
        Section("Resources");

        if (ph != null) {
            GUILayout.Label($"HP: {ph.CurrentHP} / {ph.MaxHP}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Full Heal")) ph.Heal(ph.MaxHP);
            if (GUILayout.Button("-25 HP")) ph.TakeDamage(25, ph.transform.position);
            GUILayout.EndHorizontal();
        }
        else {
            GUILayout.Label("Player not found.");
        }

        //  STABILITY
        if (ph != null && pc != null) {
            Section("Stability");

            GUILayout.Label($"Tier: {pc.CurrentStability}   " +
                            $"hitbox {pc.CurrentAttackScale:0.00}x   " +
                            $"cooldown {pc.CurrentCooldown:0.000}s");

            // Lands mid-band, so a stray hit does not tip it into the next tier.
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("High"))  SetPercent(ph, 1.00f);
            if (GUILayout.Button("Mid"))   SetPercent(ph, 0.50f);
            if (GUILayout.Button("Low"))   SetPercent(ph, 0.20f);
            GUILayout.EndHorizontal();
        }

        int typedLumens = IntField("Lumens", run.lumens);
        if (typedLumens != run.lumens) GameManager.Instance.AddLumens(typedLumens - run.lumens);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+15 Lumens")) GameManager.Instance.AddLumens(15); ;
        if (GUILayout.Button("-15 Lumens")) GameManager.Instance.TakeLumens(15); ;
        GUILayout.EndHorizontal();

        //  ABILITIES 
        Section("Abilities");

        run.isDashUnlocked = Toggle("Dash", run.isDashUnlocked);
        run.isDoubleJumpUnlocked = Toggle("Double Jump", run.isDoubleJumpUnlocked);
        run.isWallJumpUnlocked = Toggle("Wall Jump", run.isWallJumpUnlocked);

        //  GRANT
        Section("Grant");

        if (GUILayout.Button("Give One Of Everything")) {
            if (grantGoods != null)
                foreach (ShopGood g in grantGoods)
                    if (g != null && !g.SoldOut(run)) g.Purchase(run);

            if (grantAbilities != null)
                foreach (ActiveAbility a in grantAbilities)
                    if (a != null) run.AddAbility(a);
        }

        if (grantGoods != null) {
            foreach (ShopGood g in grantGoods) {
                if (g == null) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{g.DisplayName} ({g.OwnedCount(run)})");

                GUI.enabled = !g.SoldOut(run);
                if (GUILayout.Button("+1", GUILayout.Width(36))) g.Purchase(run);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
        }

        if (grantAbilities != null) {
            foreach (ActiveAbility a in grantAbilities) {
                if (a == null) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(a.abilityName);

                GUI.enabled = !run.OwnsAbility(a);
                if (GUILayout.Button("Give", GUILayout.Width(36))) run.AddAbility(a);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
        }

        //  TELEPORTATION
        Section("Teleport");

        if (GUILayout.Button("Teleport To Checkpoint"))
            GameManager.Instance.GoToCheckpoint();

        //  SCENE 
        Section("Scene");

        if (GUILayout.Button("Reload Current Scene"))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        GUILayout.EndScrollView();

        // Allow dragging the window
        GUI.DragWindow();
    }

    //  HELPERS 

    // Heal to full first, so this sets an absolute level rather than subtracting from
    // wherever you happened to be. Damage is dealt through TakeDamage on purpose: it is
    // the same path the game uses, so it cannot drift from real behaviour.
    private void SetPercent(PlayerHealth ph, float percent) {
        ph.Heal(ph.MaxHP);

        int target = Mathf.RoundToInt(ph.MaxHP * Mathf.Clamp01(percent));
        int loss = ph.CurrentHP - target;

        if (loss > 0) ph.TakeDamage(loss, ph.transform.position);
    }

    private void Section(string label) {
        GUILayout.Space(6);
        GUILayout.Label($"── {label} ──");
    }

    private bool Toggle(string label, bool value) {
        return GUILayout.Toggle(value, label);
    }

    private int IntField(string label, int value) {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80));
        string input = GUILayout.TextField(value.ToString(), GUILayout.Width(60));
        GUILayout.EndHorizontal();

        // TryParse writes 0 on failure; returning it wiped lumens whenever the box was cleared.
        return int.TryParse(input, out int result) ? result : value;
    }
}