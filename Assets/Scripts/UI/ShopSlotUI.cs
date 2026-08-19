using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns the dialled-in quantity; ShopUI owns the money.
public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;

    [Header("Quantity")]
    // Reads "3/10" - buying three, ten in stock.
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button plusButton;

    [SerializeField] private Button buyButton;

    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color tooExpensiveColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color soldOutColor = new Color(1f, 1f, 1f, 0.35f);

    private ShopGood good;
    private GameRunProfile run;
    private System.Action<ShopGood, int> onBuy;
    private int quantity = 1;

    private void Awake()
    {
        buyButton.onClick.AddListener(() => onBuy?.Invoke(good, quantity));
        minusButton.onClick.AddListener(() => Step(-1));
        plusButton.onClick.AddListener(() => Step(1));
    }

    public void Bind(ShopGood item, GameRunProfile profile, System.Action<ShopGood, int> buyCallback)
    {
        good = item;
        run = profile;
        onBuy = buyCallback;
        quantity = 1;

        UpdateVisuals();
    }

    private void Step(int delta)
    {
        quantity += delta;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        int stock = good.StockRemaining(run);
        int max = good.MaxBuyable(run);

        quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, max));

        if (icon != null)
        {
            icon.sprite = good.icon;
            icon.enabled = good.icon != null;
        }

        nameText.text = good.DisplayName;
        if (descriptionText != null) descriptionText.text = good.description;

        quantityText.text = $"{quantity}/{(stock < 0 ? "∞" : stock.ToString())}";

        bool soldOut = good.SoldOut(run);
        bool canBuy = !soldOut && max >= quantity;

        priceText.text = soldOut ? "Sold" : good.TotalPrice(run, quantity).ToString();
        priceText.color = soldOut ? soldOutColor : (canBuy ? affordableColor : tooExpensiveColor);

        minusButton.interactable = quantity > 1;
        plusButton.interactable = quantity < max;
        buyButton.interactable = canBuy;
    }
}
