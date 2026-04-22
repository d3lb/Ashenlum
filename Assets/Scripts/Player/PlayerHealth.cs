using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int hp = 100;
    [SerializeField] private int maxHp = 100;

    [SerializeField] private float iFrameTime = 0.3f;
    private float iFrameTimer;
    private bool isInvincible;


    public void Update()
    {
        if (isInvincible)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0)
                isInvincible = false;
        }
    }

    public void TakeDamage(int dmg, Vector2 attackerPos)
    {
        if (isInvincible)
            return;

        hp -= dmg;

        isInvincible = true;
        iFrameTimer = iFrameTime;

        if (hp <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Player dead");
    }
}