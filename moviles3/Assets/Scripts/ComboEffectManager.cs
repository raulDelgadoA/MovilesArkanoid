using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // Necesario para URP (Universal Render Pipeline)

public class ComboEffectManager : MonoBehaviour
{
    public static ComboEffectManager Instance;

    [Header("Referencias")]
    public Volume globalVolume;
    public Camera mainCamera;
    public GameObject floatingTextPrefab;

    [Header("Configuración de Fiebre")]
    public int hitsToTriggerFever = 5;      // Cuántos ladrillos seguidos para activar
    public float comboResetTime = 1.5f;     // Tiempo antes de perder el combo
    public float hueSpeed = 150f;           // Velocidad del cambio de color
    public float shakeIntensity = 0.2f;     // Cuánto tiembla la pantalla

    [Header("Configuración Texto Balatro")]
    public float maxTextScale = 10f; // Tamaño máximo
    public float scalePerHit = 0.5f;  // Cuánto crece por cada golpe extra

    [Header("Configuración de Vibración (Haptics)")]
    public long baseVibration = 20;     // Vibración inicial (muy suave, un 'tick')
    public long vibrationStep = 10;     // Cuánto sube por cada golpe
    public long maxVibration = 80;      // Tope máximo (para no molestar)

    [Header("Estado (Solo lectura)")]
    public int currentCombo = 0;
    public bool isFeverMode = false;

    // Variables internas
    private ColorAdjustments colorAdj;
    private ChromaticAberration chromAb; // Opcional: efecto glitch
    private float comboTimer;
    private float currentHue = 0;
    private Vector3 originalCamPos;

    // Referencia al efecto Gyro para no pelearse con él
    private CameraGyroEffect gyroScript;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Obtener los efectos del volumen
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out colorAdj);
            globalVolume.profile.TryGet(out chromAb);
        }

        if (mainCamera != null)
        {
            gyroScript = mainCamera.GetComponent<CameraGyroEffect>();
        }
    }

    void Update()
    {
        // 1. Lógica del temporizador de combo
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
        }
        else if (currentCombo > 0)
        {
            // Se acabó el tiempo, reseteamos combo
            ResetCombo();
        }

        // 2. EFECTOS VISUALES
        if (isFeverMode)
        {
            HandleFeverEffects();
        }
        else
        {
            HandleNormalReturn();
        }
    }

    // --- NUEVO: RECIBIMOS LA POSICIÓN ---
    public void RegisterHit(Vector3 position, int scoreAmount)
    {
        currentCombo++;
        comboTimer = comboResetTime;

        if (currentCombo >= hitsToTriggerFever) isFeverMode = true;

        // SPAWNEAR TEXTO
        SpawnFloatingText(position, scoreAmount);
        TriggerVibration();
    }

    void SpawnFloatingText(Vector3 pos, int score)
    {
        if (floatingTextPrefab == null) return;

        Vector3 spawnPos;
        // Rotación base mirando al cielo
        Quaternion rotation = Quaternion.Euler(90, 0, 0);

        if (isFeverMode)
        {
            // MODO ÉPICO: CENTRO + CAOS
            Vector3 centerScreen = new Vector3(0, 4f, 0); // Muy alto
            Vector2 randomCircle = Random.insideUnitCircle * 2.5f; // Más dispersión
            spawnPos = centerScreen + new Vector3(randomCircle.x, 0, randomCircle.y);

            // !! TRUCO DE EPICIDAD 1: ROTACIÓN ALEATORIA (Z) !!
            // Inclinamos los números a los lados (-25 a 25 grados) para que parezca
            // que caen desordenados sobre la mesa.
            float randomZ = Random.Range(-25f, 25f);
            rotation = Quaternion.Euler(90, 0, randomZ);
        }
        else
        {
            // MODO NORMAL: En el ladrillo, ordenadito
            spawnPos = pos + new Vector3(0, 2f, 0);
        }

        GameObject floatText = Instantiate(floatingTextPrefab, spawnPos, rotation);

        // CÁLCULO DE TAMAÑO
        float scale = 1f + (currentCombo * scalePerHit);

        // !! TRUCO DE EPICIDAD 2: TAMAÑO COLOSAL !!
        // Antes era 1.5f, ahora multiplicamos por 3.0f si es fiebre.
        if (isFeverMode) scale *= 3.0f;

        //if (scale > maxTextScale) scale = maxTextScale;

        FloatingScore script = floatText.GetComponent<FloatingScore>();
        if (script != null)
        {
            script.Setup(score, scale, isFeverMode);
        }
    }

    // Llamar a esto cada vez que rompes un ladrillo
    public void RegisterHit()
    {
        currentCombo++;
        comboTimer = comboResetTime;

        // Si superamos el umbral, activamos el modo FIEBRE
        if (currentCombo >= hitsToTriggerFever)
        {
            isFeverMode = true;
        }

        // Pequeño sacudida extra con cada golpe (Golpe seco)
        if (isFeverMode) AddShake(0.1f);
    }

    // --- FUNCIÓN NUEVA PARA VIBRAR ---
    void TriggerVibration()
    {
        // Cálculo: Base + (Combo * Paso)
        long duration = baseVibration + (currentCombo * vibrationStep);

        // Límite: Que nunca supere el máximo (ej. 80ms)
        if (duration > maxVibration) duration = maxVibration;

        // ¡Zumbido!
        Vibration.Vibrate(duration);
    }

    void ResetCombo()
    {
        currentCombo = 0;
        isFeverMode = false;
    }

    void HandleFeverEffects()
    {
        // A) HUE SHIFT (Colores locos)
        if (colorAdj != null)
        {
            // Movemos el Hue continuamente
            currentHue += hueSpeed * Time.deltaTime;
            if (currentHue > 180) currentHue = -180; // Loop de color

            colorAdj.hueShift.value = currentHue;
        }

        // B) CHROMATIC ABERRATION (Distorsión de colores en bordes)
        if (chromAb != null)
        {
            chromAb.intensity.value = Mathf.Lerp(chromAb.intensity.value, 1f, Time.deltaTime * 5f);
        }

        // C) SCREEN SHAKE (Vibración)
        if (mainCamera != null)
        {
            // Generamos una posición aleatoria pequeña
            Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;

            // Aplicamos sobre la posición actual (respetando el Gyro si existe)
            // Nota: Esto es un efecto visual momentáneo
            mainCamera.transform.position += shakeOffset;
        }
    }

    void HandleNormalReturn()
    {
        // Volver suavemente a la normalidad
        if (colorAdj != null)
        {
            // Lerp hacia 0 (color normal)
            colorAdj.hueShift.value = Mathf.Lerp(colorAdj.hueShift.value, 0f, Time.deltaTime * 2f);
        }

        if (chromAb != null)
        {
            chromAb.intensity.value = Mathf.Lerp(chromAb.intensity.value, 0f, Time.deltaTime * 2f);
        }

        // El shake se detiene solo porque dejamos de sumar el offset aleatorio
    }

    // Helper para sacudidas puntuales
    void AddShake(float amount)
    {
        if (mainCamera != null)
            mainCamera.transform.position += Random.insideUnitSphere * amount;
    }
}