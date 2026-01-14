using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSound : MonoBehaviour
{
    void Start()
    {
        Toggle miToggle = GetComponent<Toggle>();

        miToggle.onValueChanged.AddListener((valor) =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUIClick();
            }
        });
    }
}