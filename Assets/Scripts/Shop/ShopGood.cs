using UnityEngine;

// Anything a shop can put on a shelf. Upgrades and lumen bundles are both this, so the
// shop keeps one list instead of two.
public abstract class ShopGood : ScriptableObject
{
    public string id;
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    public string Id => string.IsNullOrEmpty(id) ? name : id;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

    public abstract int PriceFor(GameRunProfile run);
    public abstract bool SoldOut(GameRunProfile run);
    public abstract void Purchase(GameRunProfile run);

    // How many you already hold or have bought.
    public abstract int OwnedCount(GameRunProfile run);

    // How many more the shop will sell. -1 means unlimited.
    public abstract int StockRemaining(GameRunProfile run);

    // Price of the next one if you had already bought "extra" more this visit.
    // Only matters for goods whose price climbs, like strength.
    public virtual int PriceAfter(GameRunProfile run, int extra) => PriceFor(run);

    public int TotalPrice(GameRunProfile run, int quantity)
    {
        int total = 0;
        for (int i = 0; i < quantity; i++) total += PriceAfter(run, i);
        return total;
    }

    // Most you could walk away with right now - limited by stock AND by lumens.
    public int MaxBuyable(GameRunProfile run)
    {
        int stock = StockRemaining(run);
        int purse = run.lumens;
        int count = 0;

        while (stock < 0 || count < stock)
        {
            int price = PriceAfter(run, count);
            if (price <= 0 || price > purse) break;
            purse -= price;
            count++;
            if (count >= 99) break;
        }
        return count;
    }
}
