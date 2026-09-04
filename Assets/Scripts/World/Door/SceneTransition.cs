using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour {
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string targetEntranceId;

    private bool loading;

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player") || loading) return;
        loading = true;

        GameManager.Instance.GoToScene(sceneToLoad, targetEntranceId);
    }
}