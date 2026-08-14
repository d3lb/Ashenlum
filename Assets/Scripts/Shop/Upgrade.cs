using UnityEngine;

public enum UpgradeType
{
    MaxHealth,
    BurstHeal,
    Dash
}

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Ashenlum/Upgrade")]
public class Upgrade : ScriptableObject
{
    public string id;
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("Effect")]
    public UpgradeType type;
    // 25 for +25 max HP, 0.05 for +5% burst heal, 1 for +1 dash.
    public float amount = 1f;

    [Header("Cost")]
    public int baseCost = 100;
    // Added to the cost for every purchase already made.
    public int costIncrease = 50;
    // 0 = buy as many as you like.
    public int maxPurchases = 1;

    public string Id => string.IsNullOrEmpty(id) ? name : id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    public int CostAt(int timesPurchased) => baseCost + costIncrease * timesPurchased;

    public bool SoldOutAt(int timesPurchased) =>
        maxPurchases > 0 && timesPurchased >= maxPurchases;
}
