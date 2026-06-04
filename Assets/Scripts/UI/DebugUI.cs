using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    [SerializeField] private PlayerMovement player;
    [SerializeField] private TextMeshProUGUI text;

    [SerializeField] private PlayerState state;
    [SerializeField] private PlayerAbility pAbility;
    [SerializeField] private EnemyState Estate;
    [SerializeField] private CameraManager cameraa;

    void Update()
    {
        text.text =
            "State: " + state.CurrentState + "\n" +
            "Jumps: " + player.JumpNumber + "\n" +
            "Burst Cooldown: " + (int)(pAbility.CooldownPercent * 100) + "%\n" +
            "Room: " + (cameraa.CurrentRoom != null ? cameraa.CurrentRoom.name : "None");/* + "\n" + 
            "Flying Enemy State: " + Estate.CurrentState; */
    }
}