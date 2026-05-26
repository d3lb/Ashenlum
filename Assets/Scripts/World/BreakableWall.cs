using UnityEngine;

public class BreakableWall : MonoBehaviour
{
    [SerializeField] private int hitsToBreak = 4;

    public bool TakeDamage()
    {
        hitsToBreak--;

        if (hitsToBreak <= 0)
        {
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}