// One gate for "is something already in control of the screen".
public static class UIState
{
    public static bool PanelOpen =>
        InventoryUI.IsOpen ||
        RestPointUI.IsOpen ||
        ShopUI.IsOpen ||
        ConfirmModal.IsOpen ||
        SettingsPanel.IsOpen ||
        DialogueManager.IsDialogueActive ||
        (PauseManager.Instance != null && PauseManager.Instance.IsPaused);

    // Set by cutscenes and credits, which are not panels.
    public static bool CutsceneActive;

    public static bool Busy =>
        PanelOpen || CutsceneActive || CheckPoint.Resting || Transitioning;

    private static bool Transitioning =>
        GameManager.Instance != null &&
        GameManager.Instance.activeRun.isTransitioningScenes;
}
