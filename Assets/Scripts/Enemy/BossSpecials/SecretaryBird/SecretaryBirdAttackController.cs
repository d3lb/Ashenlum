using UnityEngine;

public class SecretaryBirdAttackController : MonoBehaviour
{
    [SerializeField] private SecretaryBirdState state;

    [Header("Dash")]
    [SerializeField] private GameObject dashHitboxLeft;
    [SerializeField] private GameObject dashHitboxRight;

    [Header("Dive")]
    [SerializeField] private GameObject diveHitbox;

    private void Awake()
    {
        DisableAllHitboxes();
    }

    public void EnableDashHitbox()
    {
        if (state.IsFacingRight)
            dashHitboxRight.SetActive(true);
        else
            dashHitboxLeft.SetActive(true);
    }

    public void DisableDashHitbox()
    {
        dashHitboxLeft.SetActive(false);
        dashHitboxRight.SetActive(false);
    }

    public void EnableDiveHitbox()
    {
        diveHitbox.SetActive(true);
    }

    public void DisableDiveHitbox()
    {
        diveHitbox.SetActive(false);
    }

    public void DisableAllHitboxes()
    {
        dashHitboxLeft.SetActive(false);
        dashHitboxRight.SetActive(false);
        diveHitbox.SetActive(false);
    }
}