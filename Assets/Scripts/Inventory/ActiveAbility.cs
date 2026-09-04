using UnityEngine;

// An equippable ability as an asset, so adding a new one is a file and not a code change.
public abstract class ActiveAbility : ScriptableObject {
    // Saved by id, same as ShopGood. Leave blank to use the asset name.
    public string id;
    public string Id => string.IsNullOrEmpty(id) ? name : id;

    public string abilityName = "New Ability";
    [TextArea] public string description;
    public Sprite icon;

    public float chargeTime = 1f;
    public float cooldown = 3f;

    public abstract void Fire(PlayerActiveAbility user);
}
