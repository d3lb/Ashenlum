using UnityEngine;

// A reusable, designer-authored conversation. Create assets via
// Assets ▸ Create ▸ Ashenlum ▸ Dialogue, fill in the sentences, then drop the
// asset onto any NPCInteractable. Kept intentionally data-only so it stays
// fully decoupled from the runtime (the DialogueManager just reads it).
[CreateAssetMenu(fileName = "New Dialogue", menuName = "Ashenlum/Dialogue")]
public class Dialogue : ScriptableObject
{
    [Tooltip("Optional speaker name shown above the text. Leave blank to hide the name line.")]
    public string speakerName;

    [Tooltip("One entry per line. Shown in order; press E to reveal/advance.")]
    [TextArea(2, 5)]
    public string[] sentences;
}
