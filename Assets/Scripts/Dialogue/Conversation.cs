using UnityEngine;

[CreateAssetMenu(fileName = "New Conversation", menuName = "Ashenlum/Conversation")]
public class Conversation : ScriptableObject
{
    public string speakerName;

    [TextArea(2, 5)]
    public string[] sentences;
}
