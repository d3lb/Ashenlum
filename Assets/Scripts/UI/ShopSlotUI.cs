using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row in the shop list. Renders what ShopUI hands it and reports the click back.
public class ShopSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;

    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color tooExpensiveColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color soldOutColor = new Color(1f, 1f, 1f, 0.35f);

    private Upgrade upgrade;
    private System.Action<Upgrade> onBuy;

    private void Awake()
    {
        buyButton.onClick.AddListener(() => onBuy?.Invoke(upgrade));
    }

    public void Bind(Upgrade item, int timesPurchased, int playerLumens, System.Action<Upgrade> buyCallback)
    {
        upgrade = item;
        onBuy = buyCallback;

        bool soldOut = item.SoldOutAt(timesPurchased);
        int cost = item.CostAt(timesPurchased);
        bool affordable = playerLumens >= cost;

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
        }

        nameText.text = item.DisplayName;
        if (descriptionText != null) descriptionText.text = item.description;

        if (soldOut)
        {
            costText.text = "Sold";
            costText.color = soldOutColor;
        }
        else
        {
            costText.text = cost.ToString();
            costText.color = affordable ? affordableColor : tooExpensiveColor;
        }

        buyButton.interactable = !soldOut && affordable;
    }
}
