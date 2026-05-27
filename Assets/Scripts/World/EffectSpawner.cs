using UnityEngine;

public class EffectSpawner : MonoBehaviour
{

    [Header("Hit VFX")]

    [SerializeField] private GameObject bigHitPrefab;
    [SerializeField] private GameObject smallHitPrefab;
    [SerializeField] private float smallOffset = 0.15f;
    [SerializeField] private float randomAngle = 15f;

    public void SpawnHitEffect(Vector3 pos, bool vertical)
    {
        Quaternion bigRot =
            vertical
            ? Quaternion.Euler(0, 0, 90)
            : Quaternion.identity;

        bigRot *= Quaternion.Euler(
            0,
            0,
            Random.Range(
                -randomAngle,
                randomAngle
            )
        );

        Instantiate(
            bigHitPrefab,
            pos,
            bigRot
        );

        for (int i = 0; i < 2; i++)
        {
            Quaternion smallRot =
                vertical
                ? Quaternion.Euler(0, 0, 90)
                : Quaternion.identity;

            smallRot *= Quaternion.Euler(
                0,
                0,
                Random.Range(
                    -randomAngle * 2,
                    randomAngle * 2
                )
            );

            Instantiate(
                smallHitPrefab,
                pos +
                Random.insideUnitSphere *
                smallOffset,
                smallRot
            );
        }
    }
}
