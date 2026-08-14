using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CheatMenu : MonoBehaviour
{
    [Header("Toggle Key")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    private bool isOpen = false;
    private Vector2 scrollPos;

    private Rect windowRect = new Rect(20, 20, 280, 500);
    private Camera cam;

    private void Update()
    {
        if (cam == null)
            cam = FindFirstObjectByType<Camera>();


        if (Input.GetKeyDown(toggleKey))
            isOpen = !isOpen;

        if (isOpen && Input.GetMouseButtonDown(2))
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = Mathf.Abs(cam.transform.position.z);

                Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);
                worldPos.z = player.transform.position.z;

                player.transform.position = worldPos;
            }
        }
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        windowRect = GUI.Window(0, windowRect, DrawWindow, "Cheat Menu (F1)");
    }

    private void DrawWindow(int id)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerHealth ph = player?.GetComponent<PlayerHealth>();

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GameRunProfile run = GameManager.Instance?.activeRun;
        if (run == null)
        {
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

        if (ph != null)
        {
            GUILayout.Label($"HP: {ph.CurrentHP} / {ph.MaxHP}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Full Heal"))
            {
                ph.Heal(ph.MaxHP);
                run.currentHp = ph.CurrentHP;
            }
            if (GUILayout.Button("-25 HP"))
            {
                ph.TakeDamage(25, ph.transform.position);
                run.currentHp = ph.CurrentHP;
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("Player not found.");
        }

        run.lumens = IntField("Lumens", run.lumens);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+15 Lumens")) GameManager.Instance.AddLumens(15); ;
        if (GUILayout.Button("-15 Lumens")) GameManager.Instance.TakeLumens(15); ;
        GUILayout.EndHorizontal();

        //  ABILITIES 
        Section("Abilities");

        run.isDashUnlocked = Toggle("Dash", run.isDashUnlocked);
        run.isDoubleJumpUnlocked = Toggle("Double Jump", run.isDoubleJumpUnlocked);
        run.isWallJumpUnlocked = Toggle("Wall Jump", run.isWallJumpUnlocked);

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

    private void Section(string label)
    {
        GUILayout.Space(6);
        GUILayout.Label($"── {label} ──");
    }

    private bool Toggle(string label, bool value)
    {
        return GUILayout.Toggle(value, label);
    }

    private int IntField(string label, int value, System.Action<int> onChanged = null)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(80));
        string input = GUILayout.TextField(value.ToString(), GUILayout.Width(60));
        GUILayout.EndHorizontal();
        if (int.TryParse(input, out int result) && result != value)
            onChanged?.Invoke(result);
        return result;
    }
}