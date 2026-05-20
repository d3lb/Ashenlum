using UnityEngine;

public class BreakableWall : MonoBehaviour
{
    [SerializeField] private int hp = 6;

    public bool TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            Destroy(gameObject);
            return true;
        }
        return false;
    }
}