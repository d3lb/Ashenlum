using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float waitTime = 0.5f;

    private Transform target;
    private float waitTimer;

    private void Start()
    {
        target = pointB;
    }

    private void Update()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.05f)
        {
            target = target == pointA ? pointB : pointA;
            waitTimer = waitTime;
        }
    }
}