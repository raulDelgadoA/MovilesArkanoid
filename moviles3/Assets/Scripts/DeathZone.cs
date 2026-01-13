using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Si lo que entra es la bola
        if (other.CompareTag("Ball"))
        {
            // Avisamos al GameManager
            GameManager.Instance.LoseLife();
        }
    }
}