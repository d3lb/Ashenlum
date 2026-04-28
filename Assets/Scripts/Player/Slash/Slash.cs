using UnityEngine;

public class Slash : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.1f;

    private void OnEnable()
    {
        Destroy(gameObject, lifeTime);
    }
}