using UnityEngine;

[CreateAssetMenu(fileName = "Strength", menuName = "Ashenlum/Strength Upgrade")]
public class StrengthUpgrade : ShopGood {
    public int damagePerLevel = 1;
    public int maxLevel = 5;

    [Header("Cost")]
    public int baseCost = 150;
    public int costIncrease = 100;

    public override int PriceFor(GameRunProfile run) =>
        baseCost + costIncrease * run.strengthLevel;

    public override int PriceAfter(GameRunProfile run, int extra) =>
        baseCost + costIncrease * (run.strengthLevel + extra);

    public override bool SoldOut(GameRunProfile run) =>
        maxLevel > 0 && run.strengthLevel >= maxLevel;

    public override void Purchase(GameRunProfile run) => run.strengthLevel++;

    public override int OwnedCount(GameRunProfile run) => run.strengthLevel;

    public override int StockRemaining(GameRunProfile run) =>
        maxLevel > 0 ? Mathf.Max(0, maxLevel - run.strengthLevel) : -1;
}
