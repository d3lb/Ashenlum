using UnityEngine;

public class Shop : Interactable
{
    [SerializeField] private Conversation greeting;
    [SerializeField] private Upgrade[] stock;

    protected override string PromptVerb => "Trade";

    protected override bool CanInteract => !DialogueManager.IsDialogueActive && !ShopUI.IsOpen;

    protected override void Interact()
    {
        if (greeting != null && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(greeting, OpenShop);
        else
            OpenShop();
    }

    private void OpenShop()
    {
        if (ShopUI.Instance != null) ShopUI.Instance.Open(stock);
    }
}
