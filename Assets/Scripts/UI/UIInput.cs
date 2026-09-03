using UnityEngine;

// The "next / confirm" press, in one place.
//
// Players do not read a manual before pressing something to skip a line - they try
// whatever is under their hand. Accepting all of them costs nothing and removes the
// moment where someone thinks the game has frozen.
public static class UIInput
{
    public static bool AdvancePressed =>
        Input.GetKeyDown(KeyCode.E) ||
        Input.GetKeyDown(KeyCode.Space) ||
        Input.GetKeyDown(KeyCode.Return) ||
        Input.GetKeyDown(KeyCode.KeypadEnter) ||
        Input.GetMouseButtonDown(0);
}
