using UnityEngine;

// A pouch of lumens you buy at a premium. Death takes your loose lumens; it cannot take
// a bundle. Cash it in later to get the value back.
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

    public override int OwnedCount(GameRunProfile run) => run.BundleCount(Id);

    public override int StockRemaining(GameRunProfile run) =>
        stockLimit > 0 ? Mathf.Max(0, stockLimit - run.BundleCount(Id)) : -1;
}
