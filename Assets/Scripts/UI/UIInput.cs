using UnityEngine;

public static class UIInput {
    public static bool AdvancePressed =>
        Input.GetKeyDown(KeyCode.E) ||
        Input.GetKeyDown(KeyCode.Space) ||
        Input.GetKeyDown(KeyCode.Return) ||
        Input.GetKeyDown(KeyCode.KeypadEnter) ||
        Input.GetMouseButtonDown(0);
}
