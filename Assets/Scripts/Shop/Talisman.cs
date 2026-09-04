using UnityEngine;

public enum TalismanType {
    MaxHealth,
    BurstHeal,
    Dash
}

// Owned forever, two equipped. Bonuses are derived, never banked.
[CreateAssetMenu(fileName = "New Talisman", menuName = "Ashenlum/Talisman")]
public class Talisman : ShopGood {
    [Header("Effect")]
    public TalismanType type;
    // 25 for +25 max HP, 0.05 for +5% burst heal, 1 for +1 dash.
    public float amount = 1f;

    [Header("Cost")]
    public int price = 100;

    public override int PriceFor(GameRunProfile run) => price;

    public override bool SoldOut(GameRunProfile run) => run.Owns(this);

    public override void Purchase(GameRunProfile run) => run.AddTalisman(this);

    public override int OwnedCount(GameRunProfile run) => run.Owns(this) ? 1 : 0;

    public override int StockRemaining(GameRunProfile run) => run.Owns(this) ? 0 : 1;
}
