using UnityEngine;

public enum PowerUpType
{
    None,
    ExtraBall,
    SpeedUp,
    SlowDown,
    SafetyNet 
}

public class BrickController : MonoBehaviour
{
    public PowerUpType currentPowerUp = PowerUpType.None;

    [Header("PowerUp Colors")]
    public Color normalColor = Color.white;
    public Color extraBallColor = Color.green;
    public Color speedUpColor = Color.red;
    public Color slowDownColor = Color.blue;
    public Color safetyNetColor = Color.cyan;

    private float emissionIntensity = 0.5f;

    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    void UpdateVisuals()
    {
        if (rend == null) return;

        Color targetColor = normalColor; // Por defecto

        switch (currentPowerUp)
        {
            case PowerUpType.ExtraBall: targetColor = extraBallColor; break;
            case PowerUpType.SpeedUp: targetColor = speedUpColor; break;
            case PowerUpType.SlowDown: targetColor = slowDownColor; break;
            case PowerUpType.SafetyNet: targetColor = safetyNetColor; break;
        }

        // 1. Cambiamos el color base (para que se vea bonito)
        rend.material.color = targetColor;

        // 2. ¡EL TRUCO DEL NEÓN!
        // Activamos la emisión y le damos el color multiplicado por la intensidad
        rend.material.EnableKeyword("_EMISSION");
        rend.material.SetColor("_EmissionColor", targetColor * emissionIntensity);
    }

    public void SetupPowerUp(PowerUpType type)
    {
        currentPowerUp = type;
        UpdateVisuals();
    }
}