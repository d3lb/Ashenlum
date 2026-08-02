using UnityEngine;

public class BreakableWall : MonoBehaviour, IDamageable
{
    [SerializeField] private string wallID;

    [SerializeField] private int hitsToBreak = 4;

    private void Start()
    {
        if (
            GameManager.Instance
            .IsWallBroken(wallID)
        )
        {
            Destroy(gameObject);
        }
    }

    public bool TakeDamage(int damage, Vector2 attackerPos)
    {
        hitsToBreak--;

        if (hitsToBreak <= 0)
        {
            GameManager.Instance
                .RegisterBrokenWall(wallID);

            Destroy(gameObject);

            return true;
        }

        return false;
    }
}