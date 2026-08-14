using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform slotParent;
    [SerializeField] private ShopSlotUI slotPrefab;
    [SerializeField] private LumenUI lumenUI;

    [Header("Bank")]
    [SerializeField] private TMP_Text bankedText;
    [SerializeField] private Button depositButton;
    [SerializeField] private Button withdrawButton;

    [Header("Close")]
    [SerializeField] private Button closeButton;
    [SerializeField] private bool closeWithEscape = true;

    private readonly List<ShopSlotUI> spawned = new();
    private Upgrade[] stock;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsOpen = false;

        shopPanel.SetActive(false);

        if (depositButton != null)  depositButton.onClick.AddListener(DepositAll);
        if (withdrawButton != null) withdrawButton.onClick.AddListener(WithdrawAll);
        if (closeButton != null)    closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        if (!IsOpen) return;

        IsOpen = false;
        lumenUI?.Hide();
        TimeManager.Release(this);
    }

    private void Update()
    {
        if (IsOpen && closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open(Upgrade[] items)
    {
        if (IsOpen) return;

        stock = items;
        IsOpen = true;
        shopPanel.SetActive(true);
        lumenUI?.Show();

        BuildList();
        Refresh();

        TimeManager.Freeze(this);
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        shopPanel.SetActive(false);
        lumenUI?.Hide();

        TimeManager.Release(this);
    }

    private void BuildList()
    {
        for (int i = slotParent.childCount - 1; i >= 0; i--)
            Destroy(slotParent.GetChild(i).gameObject);
        spawned.Clear();

        if (stock == null) return;

        foreach (Upgrade item in stock)
        {
            if (item == null) continue;
            spawned.Add(Instantiate(slotPrefab, slotParent));
        }
    }

    private void Refresh()
    {
        var run = GameManager.Instance.activeRun;

        for (int i = 0; i < spawned.Count && i < stock.Length; i++)
            spawned[i].Bind(stock[i], run.TimesPurchased(stock[i].Id), run.lumens, Buy);

        if (bankedText != null) bankedText.text = run.bankedLumens.ToString();
        if (depositButton != null)  depositButton.interactable  = run.lumens > 0;
        if (withdrawButton != null) withdrawButton.interactable = run.bankedLumens > 0;
    }

    private void Buy(Upgrade item)
    {
        var run = GameManager.Instance.activeRun;
        int cost = item.CostAt(run.TimesPurchased(item.Id));

        if (item.SoldOutAt(run.TimesPurchased(item.Id))) return;
        if (run.lumens < cost) return;

        GameManager.Instance.TakeLumens(cost);
        run.ApplyUpgrade(item);

        // Max health has to grow on the live player, not just in the profile.
        if (item.type == UpgradeType.MaxHealth)
            FindFirstObjectByType<PlayerHealth>()?.RefreshMaxHealth();

        Refresh();
    }

    private void DepositAll()
    {
        var run = GameManager.Instance.activeRun;
        if (run.lumens <= 0) return;

        int amount = run.lumens;
        GameManager.Instance.TakeLumens(amount);
        run.bankedLumens += amount;
        Refresh();
    }

    private void WithdrawAll()
    {
        var run = GameManager.Instance.activeRun;
        if (run.bankedLumens <= 0) return;

        int amount = run.bankedLumens;
        run.bankedLumens = 0;
        GameManager.Instance.AddLumens(amount);
        Refresh();
    }
}
