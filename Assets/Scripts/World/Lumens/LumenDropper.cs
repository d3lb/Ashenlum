using UnityEngine;

public class LumenDropper : MonoBehaviour {
    [Header("Drop Settings")]
    [SerializeField] private GameObject lumenPrefab;
    [SerializeField] private int minDrop = 4;
    [SerializeField] private int maxDrop = 6;

    public void Drop() {
        int amount = Random.Range(minDrop, maxDrop + 1);

        for (int i = 0; i < amount; i++) {
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 0.3f;
            Instantiate(lumenPrefab, spawnPos, Quaternion.identity);
        }
    }
}