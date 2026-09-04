using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Opens after the rest has already happened.
public class RestPointUI : MonoBehaviour
{
    public static RestPointUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;

    [Header("Views")]
    [SerializeField] private GameObject mainView;
    [SerializeField] private GameObject travelView;

    [Header("Buttons")]
    [SerializeField] private Button teleportButton;
    [SerializeField] private Button loadoutButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button returnButton;

    [Header("Travel list")]
    [SerializeField] private CheckpointDirectory directory;
    [SerializeField] private Transform travelListParent;
    [SerializeField] private TravelEntryUI travelEntryPrefab;

    [Header("Loadout")]
    [SerializeField] private InventoryUI inventory;

    private CheckPoint current;
    private readonly List<GameObject> spawnedEntries = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsOpen = false;

        if (teleportButton != null) teleportButton.onClick.AddListener(ShowTravel);
        if (loadoutButton != null)  loadoutButton.onClick.AddListener(Loadout);
        if (leaveButton != null)    leaveButton.onClick.AddListener(Close);
        if (returnButton != null)   returnButton.onClick.AddListener(ShowMain);

        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Torn down while open must not leave the game frozen.
    private void OnDisable()
    {
        if (!IsOpen) return;

        IsOpen = false;
        TimeManager.Release(this);
    }

    private void Update()
    {
        if (!IsOpen || !Input.GetKeyDown(KeyCode.Escape)) return;

        if (travelView != null && travelView.activeSelf) ShowMain();
        else                                             Close();
    }

    public void Open(CheckPoint checkpoint)
    {
        if (IsOpen) return;

        current = checkpoint;

        if (panel != null) panel.SetActive(true);
        IsOpen = true;

        TimeManager.Freeze(this);

        ShowMain();
    }

    private void ShowMain()
    {
        if (mainView != null)   mainView.SetActive(true);
        if (travelView != null) travelView.SetActive(false);

        ClearEntries();

        if (teleportButton != null) teleportButton.Select();
    }

    private void ShowTravel()
    {
        if (mainView != null)   mainView.SetActive(false);
        if (travelView != null) travelView.SetActive(true);

        BuildTravelList();

        if (returnButton != null) returnButton.Select();
    }

    private void BuildTravelList()
    {
        ClearEntries();

        if (directory == null || travelListParent == null || travelEntryPrefab == null)
        {
            Debug.LogError("[RestPointUI] Travel list is missing its Directory, List Parent " +
                           "or Entry Prefab.", this);
            return;
        }

        var run = GameManager.Instance.activeRun;
        List<CheckpointDirectory.Entry> found = directory.Discovered(run.openedCheckpoints);

        string hereId = current != null ? current.CheckpointEntranceId : null;

        foreach (CheckpointDirectory.Entry entry in found)
        {
            CheckpointDirectory.Entry captured = entry;
            bool isHere = entry.id == hereId;

            TravelEntryUI row = Instantiate(travelEntryPrefab, travelListParent, false);
            row.Bind(entry.displayName, isHere, isHere ? null : () => Travel(captured));

            spawnedEntries.Add(row.gameObject);
        }

        // Fitters resolve a frame late, so the first open would stack the rows.
        RebuildLayout(travelListParent);
    }

    // From the outermost layout group, so nested content resolves.
    private static void RebuildLayout(Transform from)
    {
        RectTransform top = null;

        for (Transform p = from; p != null; p = p.parent)
            if (p is RectTransform rect && p.GetComponent<LayoutGroup>() != null)
                top = rect;

        if (top == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(top);
    }

    private void Travel(CheckpointDirectory.Entry entry)
    {
        if (entry == null) return;

        // Closed first, so the freeze releases before the scene load.
        Close();
        GameManager.Instance.TravelToCheckpoint(entry.scene, entry.id);
    }

    private void ClearEntries()
    {
        foreach (GameObject go in spawnedEntries)
            if (go != null) Destroy(go);

        spawnedEntries.Clear();
    }

    private void Loadout()
    {
        if (inventory == null) inventory = FindFirstObjectByType<InventoryUI>();

        Close();
        inventory?.Open();
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        current = null;

        ClearEntries();

        if (panel != null) panel.SetActive(false);
        TimeManager.Release(this);
    }
}
