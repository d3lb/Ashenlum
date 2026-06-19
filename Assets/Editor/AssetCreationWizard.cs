using UnityEngine;

// NOTE: The old coordinate-guessing wizard has been replaced by the two-step
// pipeline:  Ashenlum/1. Remove White Background  →  Ashenlum/2. Smart Slice Sheet.
// Only the palette asset type is kept here for compatibility.
[CreateAssetMenu(fileName = "ShatteredGrovePalette", menuName = "Ashenlum/Shattered Grove Palette")]
public class ShatteredGrovePalette : ScriptableObject
{
    public Color[] colors;
}
