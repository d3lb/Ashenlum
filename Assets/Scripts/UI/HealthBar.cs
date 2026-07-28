using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider easeHealthSlider;
    [SerializeField] private RectTransform barTransform;
    [SerializeField] private float widthPerHP = 2f;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 5f;

    private PlayerHealth playerHealth;

    private void Start()
    {
        FindPlayer();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady += FindPlayer;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady -= FindPlayer;
    }

    private void FindPlayer()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            float percent = (float)playerHealth.CurrentHP / playerHealth.MaxHP;
            healthSlider.value = percent;
            easeHealthSlider.value = percent;
        }
    }
    private void Update()
    {
        if (playerHealth == null)
            return;

        float percent = (float)playerHealth.CurrentHP / playerHealth.MaxHP;

        healthSlider.value = percent;
        easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, percent, lerpSpeed * Time.deltaTime);

        float width = playerHealth.MaxHP * widthPerHP;
        barTransform.sizeDelta = new Vector2(width, barTransform.sizeDelta.y);
    }
}