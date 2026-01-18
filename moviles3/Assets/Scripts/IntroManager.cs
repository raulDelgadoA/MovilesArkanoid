using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoDeEspera = 4f;
    public string nombreSiguienteEscena = "MainMenuScene";

    void Start()
    {
        Invoke("CargarJuego", tiempoDeEspera);
    }

    void CargarJuego()
    {
        SceneManager.LoadScene(nombreSiguienteEscena);
    }
}