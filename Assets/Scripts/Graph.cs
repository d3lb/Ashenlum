using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public PlayerMovement player;
    public TextMeshProUGUI text;
    [SerializeField] private PlayerState state;
    [SerializeField] private EnemyState Estate;

    void Update()
    {
        text.text =
            "State: " + state.CurrentState + "\n" +
            "IsDashing: " + state.IsDashing + "\n" +
            "Jumps: " + player.JumpNumber + "\n" +
            "Enemy State: " + Estate.CurrentState + "\n";
    }
}