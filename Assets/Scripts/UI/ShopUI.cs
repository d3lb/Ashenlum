using System.Collections.Generic;
using UnityEngine;

public class ShopUI : MonoBehaviour {
    public static ShopUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform slotParent;
    [SerializeField] private ShopSlotUI slotPrefab;
    [SerializeField] private LumenUI lumenUI;

    private readonly List<ShopSlotUI> spawned = new();
    // Filtered at open: BuildList skipped blanks, Refresh did not, and the rows drifted.
    private readonly List<ShopEntry> lines = new();

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsOpen = false;

        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    private void OnDisable() {
        if (!IsOpen) return;

        IsOpen = false;
        lumenUI?.Hide();
        TimeManager.Release(this);
    }

    private void Update() {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    public void Open(ShopEntry[] entries) {
        if (IsOpen) return;

        lines.Clear();
        if (entries != null)
            foreach (ShopEntry entry in entries)
                if (entry != null && entry.good != null) lines.Add(entry);

        IsOpen = true;
        if (shopPanel != null) shopPanel.SetActive(true);
        lumenUI?.Show();

        BuildList();
        Refresh();

        TimeManager.Freeze(this);
    }

    public void Close() {
        if (!IsOpen) return;

        IsOpen = false;
        if (shopPanel != null) shopPanel.SetActive(false);
        lumenUI?.Hide();

        TimeManager.Release(this);
    }

    private void BuildList() {
        for (int i = slotParent.childCount - 1; i >= 0; i--)
            Destroy(slotParent.GetChild(i).gameObject);
        spawned.Clear();

        for (int i = 0; i < lines.Count; i++)
            spawned.Add(Instantiate(slotPrefab, slotParent));
    }

    private void Refresh() {
        var run = GameManager.Instance.activeRun;

        for (int i = 0; i < spawned.Count && i < lines.Count; i++)
            spawned[i].Bind(lines[i], run, Buy);
    }

    private void Buy(ShopEntry entry, int quantity) {
        var run = GameManager.Instance.activeRun;

        // One at a time, re-checking each step - a rising price can stop you partway.
        for (int i = 0; i < quantity; i++) {
            int price = entry.good.PriceFor(run);
            if (entry.SoldOut(run) || run.lumens < price) break;

            GameManager.Instance.TakeLumens(price);
            entry.good.Purchase(run);
            run.RecordShopSale(entry.shopId, entry.good.Id);
        }

        GameManager.Instance.MarkDirty();
        Refresh();
    }
}
