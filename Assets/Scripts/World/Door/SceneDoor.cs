using UnityEngine;

// A doorway you press E on. SceneTransition is the walk-through version; this one is for
// places the player should never wander into by accident.
public class SceneDoor : Interactable {
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string targetEntranceId;

    [SerializeField] private string verb = "Enter";

    private bool loading;

    protected override void Awake() {
        base.Awake();

        if (string.IsNullOrEmpty(sceneToLoad))
            Debug.LogError($"[SceneDoor] '{name}' has no Scene To Load.", this);
    }

    // The load takes a few frames, and E is still down for all of them.
    protected override bool CanInteract => !loading;

    protected override string PromptVerb => verb;

    protected override void Interact() {
        loading = true;
        GameManager.Instance.GoToScene(sceneToLoad, targetEntranceId);
    }
}
