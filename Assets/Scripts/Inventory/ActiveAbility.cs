using UnityEngine;

public abstract class ActiveAbility : ScriptableObject
{
    public string abilityName = "New Ability";
    [TextArea] public string description;
    public Sprite icon;

    public float chargeTime = 1f;
    public float cooldown = 3f;

    public abstract void Fire(PlayerActiveAbility user);
}
