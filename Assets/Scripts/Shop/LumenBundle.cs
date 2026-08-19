using UnityEngine;

// Bought at a premium; survives death, unlike loose lumens.
[CreateAssetMenu(fileName = "New Lumen Bundle", menuName = "Ashenlum/Lumen Bundle")]
public class LumenBundle : ShopGood
{
    // What you get back when you use it.
    public int value = 100;
    // What it costs to buy. Higher than value - that gap is the insurance premium.
    public int price = 110;

    // 0 = the shop never runs out.
    public int stockLimit = 0;

    public override int PriceFor(GameRunProfile run) => price;

    public override bool SoldOut(GameRunProfile run) => StockRemaining(run) == 0;

    public override void Purchase(GameRunProfile run) => run.AddBundle(this);

    public override int OwnedCount(GameRunProfile run) => run.BundleCount(this);

    public override int StockRemaining(GameRunProfile run) =>
        stockLimit > 0 ? Mathf.Max(0, stockLimit - run.BundleCount(this)) : -1;
}
