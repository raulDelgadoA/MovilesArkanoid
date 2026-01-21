using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class RainbowTextEffect : MonoBehaviour
{
    [Header("Configuración")]
    public float speed = 0.5f;       // Velocidad del cambio de color
    [Range(0f, 1f)] public float saturation = 1f; // Intensidad del color (1 = Neón)
    [Range(0f, 1f)] public float brightness = 1f; // Brillo

    [Header("Opciones Avanzadas")]
    public bool useGradient = true;  // Si activas esto, se verá bicolor (más chulo)
    public float gradientOffset = 0.1f; // Diferencia de color entre arriba y abajo

    private TextMeshProUGUI textMesh;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (textMesh == null) return;

        // 1. Calculamos el HUE base basado en el tiempo
        // Mathf.Repeat hace que el valor siempre vaya de 0 a 1 en bucle
        float hue = Mathf.Repeat(Time.time * speed, 1f);

        // 2. Convertimos ese HUE a un color RGB real
        Color mainColor = Color.HSVToRGB(hue, saturation, brightness);

        if (useGradient)
        {
            // EFECTO GRADIENTE (Balatro Style)
            // Calculamos un segundo color un poco desplazado en el arcoíris
            float hue2 = Mathf.Repeat(hue + gradientOffset, 1f);
            Color secondaryColor = Color.HSVToRGB(hue2, saturation, brightness);

            // Activamos el gradiente del texto
            textMesh.enableVertexGradient = true;

            // Aplicamos: Arriba el color 1, Abajo el color 2
            // Esto crea un efecto de "barrido" vertical muy bonito
            textMesh.colorGradient = new VertexGradient(mainColor, mainColor, secondaryColor, secondaryColor);
        }
        else
        {
            // EFECTO PLANO (Solo cambia el color entero)
            textMesh.color = mainColor;
        }

        // Añadir esto al final del Update:

        // Efecto de escala "latido" (Heartbeat)
        float scale = 1f + (Mathf.Sin(Time.time * 2f) * 0.05f); // Crece y decrece un 5%
        transform.localScale = Vector3.one * scale;
    }
}