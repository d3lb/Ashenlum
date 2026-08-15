using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row in the shop list. Renders what ShopUI hands it and reports the click back.
public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color tooExpensiveColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color soldOutColor = new Color(1f, 1f, 1f, 0.35f);

    private ShopGood good;
    private System.Action<ShopGood> onBuy;

    private void Awake()
    {
        buyButton.onClick.AddListener(() => onBuy?.Invoke(good));
    }

    public void Bind(ShopGood item, GameRunProfile run, System.Action<ShopGood> buyCallback)
    {
        good = item;
        onBuy = buyCallback;

        bool soldOut = item.SoldOut(run);
        int price = item.PriceFor(run);
        bool affordable = run.lumens >= price;

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
        }

        nameText.text = OwnedSuffix(item, run);
        if (descriptionText != null) descriptionText.text = item.description;

        if (soldOut)
        {
            priceText.text = "Sold";
            priceText.color = soldOutColor;
        }
        else
        {
            priceText.text = price.ToString();
            priceText.color = affordable ? affordableColor : tooExpensiveColor;
        }

        buyButton.interactable = !soldOut && affordable;
    }

    // Bundles are stackable, so show how many you're carrying.
    private static string OwnedSuffix(ShopGood item, GameRunProfile run)
    {
        if (item is LumenBundle bundle)
        {
            int held = run.BundleCount(bundle.Id);
            if (held > 0) return $"{item.DisplayName}  x{held}";
        }
        return item.DisplayName;
    }
}
