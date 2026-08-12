using UnityEngine;
using UnityEngine.UI;

// One cell in the inventory grid. Purely presentational — it never reads the
// InventoryManager itself, it just renders whatever InventoryUI hands it.
public class InventorySlotUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Image that draws the item's icon. Disabled while the slot is empty.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Optional graphic shown ONLY when the slot is empty (e.g. a dim socket).")]
    [SerializeField] private GameObject emptyGraphic;

    public Item Item { get; private set; }
    public bool IsEmpty => Item == null;

    public void SetItem(Item item)
    {
        Item = item;
        bool hasIcon = item != null && item.icon != null;

        if (iconImage != null)
        {
            iconImage.sprite  = hasIcon ? item.icon : null;
            iconImage.enabled = hasIcon;
        }

        if (emptyGraphic != null)
            emptyGraphic.SetActive(item == null);
    }

    public void Clear() => SetItem(null);
}
