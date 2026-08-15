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
}
