using TMPro;
using UnityEngine;

public class LumenUI : MonoBehaviour
{
    [SerializeField] private TMP_Text lumenText;

    private void Start()
    {
        GameManager.Instance.OnLumensChanged += UpdateText;
    }

    private void UpdateText(int amount)
    {
        lumenText.text = amount.ToString();
    }
}