using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public PlayerMovement player;
    public TextMeshProUGUI text;
    [SerializeField] private PlayerState state;


    void Update()
    {
        text.text =
            "State: " + state.CurrentState + "\n" +
            "VelX: " + player.RB.linearVelocity.x.ToString("F2") + "\n" +
            "VelY: " + player.RB.linearVelocity.y.ToString("F2") + "\n" +
            "Input: " + player.MoveInput.x + "\n" +
            "GroundTime: " + player.LastOnGroundTime.ToString("F2") + "\n" +
            "WallTime: " + player.LastOnWallTime.ToString("F2") + "\n" +
            "IsDashing: " + state.IsDashing;
    }
}