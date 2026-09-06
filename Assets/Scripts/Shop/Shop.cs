using UnityEngine;

public class Shop : Interactable {
    // Keys the per-shop sale counts, so two shops can stock the same good independently.
    [SerializeField] private string shopId;

    [SerializeField] private Conversation greeting;
    [SerializeField] private ShopEntry[] stock;

    protected override void Awake() {
        base.Awake();

        if (string.IsNullOrEmpty(shopId))
            Debug.LogError($"[Shop] '{name}' has no Shop Id, so its stock cannot be saved.", this);
    }

    protected override string PromptVerb => "Trade";

    protected override bool CanInteract => !DialogueManager.IsDialogueActive && !ShopUI.IsOpen;

    protected override void Interact() {
        if (greeting != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(greeting, OpenShop);
        else
            OpenShop();
    }

    private void OpenShop() {
        if (stock != null)
            foreach (ShopEntry entry in stock)
                if (entry != null) entry.shopId = shopId;

        if (ShopUI.Instance == null) {
            Debug.LogError($"[Shop] '{name}' pressed with no ShopUI in the scene. " +
                           "The UI Canvas prefab is missing from this scene.", this);
            return;
        }

        ShopUI.Instance.Open(stock);
    }
}
