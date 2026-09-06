using UnityEngine;

// One line of a shop's stock. The good owns the global cap, this owns how many THIS shop
// will part with - so the rest can be placed in the world without racing the shop for them.
[System.Serializable]
public class ShopEntry {
    public ShopGood good;

    // 0 or less: however many the good itself allows.
    public int limit;

    // Stamped by the Shop on open, so the id is not repeated on every line.
    [System.NonSerialized] public string shopId;

    private int SoldHere(GameRunProfile run) =>
        limit <= 0 || good == null ? 0 : run.ShopSold(shopId, good.Id);

    // The good returns -1 for unlimited; a shop limit always narrows it.
    public int StockRemaining(GameRunProfile run) {
        if (good == null) return 0;

        int own = good.StockRemaining(run);
        if (limit <= 0) return own;

        int here = Mathf.Max(0, limit - SoldHere(run));
        return own < 0 ? here : Mathf.Min(own, here);
    }

    public bool SoldOut(GameRunProfile run) =>
        good == null || good.SoldOut(run) || StockRemaining(run) == 0;

    public int MaxBuyable(GameRunProfile run) {
        if (good == null) return 0;

        int stock = StockRemaining(run);
        int max = good.MaxBuyable(run);

        return stock < 0 ? max : Mathf.Min(max, stock);
    }
}
