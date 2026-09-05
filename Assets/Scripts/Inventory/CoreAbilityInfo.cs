using UnityEngine;

// Presentation only. The unlock itself is a bool on GameRunProfile, so this is never saved
// and needs no id.
[CreateAssetMenu(fileName = "Core Ability", menuName = "Ashenlum/Core Ability")]
public class CoreAbilityInfo : ScriptableObject {
    public AbilityType ability;

    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
}
