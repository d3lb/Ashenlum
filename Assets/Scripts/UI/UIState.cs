// One place that answers "is something already in control of the screen".
//
// Every panel used to keep its own list of the others, so adding a panel meant editing
// all of them - and missing one is how two ended up open at once.
public static class UIState
{
    // Panels that own the screen. A second one opening on top of these is the bug.
    public static bool PanelOpen =>
        InventoryUI.IsOpen ||
        RestPointUI.IsOpen ||
        ShopUI.IsOpen ||
        ConfirmModal.IsOpen ||
        DialogueManager.IsDialogueActive ||
        (PauseManager.Instance != null && PauseManager.Instance.IsPaused);

    // Set by anything that takes the screen for a moment and is not a panel: credits,
    // cutscenes. A plain flag so this file never has to know what those things are.
    public static bool CutsceneActive;

    // Adds the moments the world is mid-animation and must not be interrupted.
    public static bool Busy =>
        PanelOpen || CutsceneActive || CheckPoint.Resting || Transitioning;

    private static bool Transitioning =>
        GameManager.Instance != null &&
        GameManager.Instance.activeRun.isTransitioningScenes;
}
