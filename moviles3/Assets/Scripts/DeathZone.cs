using UnityEngine;

public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Si lo que entra es la bola
        if (other.CompareTag("Ball"))
        {

            GameManager.Instance.OnBallFell(other.gameObject);
        }
    }
}