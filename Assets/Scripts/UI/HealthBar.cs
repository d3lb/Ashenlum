using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider easeHealthSlider;
    [SerializeField] private RectTransform barTransform;
    [SerializeField] private float widthPerHP = 2f;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 5f;

    private void Start()
    {
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;

        easeHealthSlider.minValue = 0f;
        easeHealthSlider.maxValue = 1f;
    }


    private void Update()
    {
        float percent = (float)playerHealth.CurrentHP / playerHealth.MaxHP;

        healthSlider.value = percent;
        easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, percent, lerpSpeed * Time.deltaTime);

        float width = playerHealth.MaxHP * widthPerHP;
        barTransform.sizeDelta = new Vector2(width, barTransform.sizeDelta.y);
    }
}