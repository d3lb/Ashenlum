using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Transform playerCheck;
	[SerializeField] private float playerCheckRange = 3f;
    [SerializeField] private LayerMask playerLayer;

    [HideInInspector] public float health;
    [HideInInspector] public float maxHealth;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    public float dirX = 0f;
    private float speed = 2.5f;

    void Start()
    {
        health = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

    }

    void Update()
    {
        Collider2D[] playerInSight = Physics2D.OverlapCircleAll(playerCheck.position, playerCheckRange, playerLayer);

        
    }

    private void FixedUpdate()
    {
        Collider2D[] playerInSight = Physics2D.OverlapCircleAll(playerCheck.position, playerCheckRange, playerLayer);

        if (playerInSight.Length > 0)
        {
            Vector2 targetPosition = new Vector2(playerInSight[0].transform.position.x, transform.position.y);
            Vector2 currentPosition = new Vector2(transform.position.x, transform.position.y);

            rb.linearVelocity = (targetPosition - currentPosition).normalized * speed;
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (rb.linearVelocity.x > 0.01f)
            sprite.flipX = false;
        else if (rb.linearVelocity.x < -0.01f)
            sprite.flipX = true;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        checkDeath();
        Debug.Log(damage);
    }

    public void CheckOverHeal()
    {
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    public void checkDeath()
    {
        if(health <= 0)
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(playerCheck.position, playerCheckRange);
    }
}
