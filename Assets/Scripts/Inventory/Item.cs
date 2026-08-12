using UnityEngine;

// Base class for every inventory item. Intentionally NOT sealed — future item
// types (Consumable, Charm, KeyItem…) should derive from this and add their own
// fields/behaviour while still fitting in an inventory slot.
// Create assets via Assets ▸ Create ▸ Ashenlum ▸ Item.
[CreateAssetMenu(fileName = "New Item", menuName = "Ashenlum/Item")]
public class Item : ScriptableObject
{
    [Tooltip("Stable unique id used for saving. Leave blank to fall back to the asset name.")]
    public string itemId;

    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("Sprite drawn in the inventory grid.")]
    public Sprite icon;

    // Never return an empty id — saves would collide on "".
    public string Id => string.IsNullOrEmpty(itemId) ? name : itemId;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
}
