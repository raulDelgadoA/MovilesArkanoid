using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UISound : MonoBehaviour
{
    void Start()
    {
        // Al iniciar, le dice al botón: "Cuando te pulsen, avisa al AudioManager"
        GetComponent<Button>().onClick.AddListener(() => {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUIClick();
        });
    }
}