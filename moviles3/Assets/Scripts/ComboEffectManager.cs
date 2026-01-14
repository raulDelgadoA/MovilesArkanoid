using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ComboEffectManager : MonoBehaviour
{
    public static ComboEffectManager Instance;

    [Header("Referencias")]
    public Volume globalVolume;
    public Camera mainCamera;
    public GameObject floatingTextPrefab;

    [Header("Audio Satisfactorio")]
    public AudioClip hitSound;
    public AudioClip feverLoopSound;
    public float pitchStep = 0.1f;
    public float maxPitch = 2.5f;

    // NOTA: Hemos quitado sfxSource porque usaremos el del Manager
    private AudioSource loopSource;     // Mantenemos este para el loop de fondo

    [Header("Configuración de Fiebre")]
    public int hitsToTriggerFever = 5;
    public float comboResetTime = 1.5f;
    public float hueSpeed = 150f;
    public float shakeIntensity = 0.2f;

    [Header("Configuración Texto")]
    public float maxTextScale = 10f;
    public float scalePerHit = 0.5f;

    [Header("Configuración de Vibración")]
    public long baseVibration = 20;
    public long vibrationStep = 10;
    public long maxVibration = 80;

    [Header("Estado (Solo lectura)")]
    public int currentCombo = 0;
    public bool isFeverMode = false;

    // Variables internas
    private ColorAdjustments colorAdj;
    private ChromaticAberration chromAb;
    private float comboTimer;
    private float currentHue = 0;
    private CameraGyroEffect gyroScript;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // --- SOLO CREAMOS EL LOOP SOURCE ---
        // El de efectos (hits) ya no hace falta crearlo, usamos el AudioManager central
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.clip = feverLoopSound;
    }

    void Start()
    {
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
        // 1. Lógica del temporizador
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
        }
        else if (currentCombo > 0)
        {
            ResetCombo();
        }

        // 2. EFECTOS VISUALES Y AUDIO LOOP
        if (isFeverMode)
        {
            HandleFeverEffects();
        }
        else
        {
            HandleNormalReturn();
        }
    }

    public void RegisterHit(Vector3 position, int scoreAmount)
    {
        currentCombo++;
        comboTimer = comboResetTime;

        if (currentCombo >= hitsToTriggerFever) isFeverMode = true;

        SpawnFloatingText(position, scoreAmount);
        TriggerVibration();

        // --- SONIDO CON PITCH ASCENDENTE ---
        PlayComboSound();
    }

    void PlayComboSound()
    {
        if (hitSound == null) return;

        // 1. Calcular el tono (Pitch)
        float newPitch = 1f + (currentCombo * pitchStep);
        if (newPitch > maxPitch) newPitch = maxPitch;

        // 2. USAR EL AUDIOMANAGER (Para que respete el volumen de opciones)
        // El método PlaySFX del manager ya gestiona el volumen por nosotros
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hitSound, newPitch);
        }
    }

    void HandleFeverEffects()
    {
        // Efectos Visuales (Igual que antes) ...
        if (colorAdj != null)
        {
            currentHue += hueSpeed * Time.deltaTime;
            if (currentHue > 180) currentHue = -180;
            colorAdj.hueShift.value = currentHue;
        }
        if (chromAb != null) chromAb.intensity.value = Mathf.Lerp(chromAb.intensity.value, 1f, Time.deltaTime * 5f);
        if (mainCamera != null) mainCamera.transform.position += Random.insideUnitSphere * shakeIntensity;


        // --- SONIDO LOOP FIEBRE ---
        // AQUI ESTA LA MAGIA: Leemos el volumen actual del AudioManager
        float volumenObjetivo = 0.5f; // Volumen base deseado para el loop (no muy alto)

        if (AudioManager.Instance != null && AudioManager.Instance.sfxSource != null)
        {
            // Multiplicamos por el volumen global. 
            // Si el slider está al 0, esto será 0. Si está al 1, será 0.5f.
            volumenObjetivo *= AudioManager.Instance.sfxSource.volume;
        }

        if (loopSource != null && feverLoopSound != null)
        {
            if (!loopSource.isPlaying)
            {
                loopSource.volume = 0;
                loopSource.Play();
            }

            // Fade In hacia el volumen objetivo (que ahora respeta el slider)
            loopSource.volume = Mathf.Lerp(loopSource.volume, volumenObjetivo, Time.deltaTime * 5f);
        }
    }

    void HandleNormalReturn()
    {
        // Volver a normalidad visual...
        if (colorAdj != null) colorAdj.hueShift.value = Mathf.Lerp(colorAdj.hueShift.value, 0f, Time.deltaTime * 2f);
        if (chromAb != null) chromAb.intensity.value = Mathf.Lerp(chromAb.intensity.value, 0f, Time.deltaTime * 2f);

        // --- PARAR SONIDO FIEBRE ---
        if (loopSource != null && loopSource.isPlaying)
        {
            // Fade Out a 0
            loopSource.volume = Mathf.Lerp(loopSource.volume, 0f, Time.deltaTime * 5f);
            if (loopSource.volume < 0.01f) loopSource.Stop();
        }
    }

    void SpawnFloatingText(Vector3 pos, int score)
    {
        if (floatingTextPrefab == null) return;

        Vector3 spawnPos;
        Quaternion rotation = Quaternion.Euler(90, 0, 0);

        if (isFeverMode)
        {
            Vector3 centerScreen = new Vector3(0, 4f, 0);
            Vector2 randomCircle = Random.insideUnitCircle * 2.5f;
            spawnPos = centerScreen + new Vector3(randomCircle.x, 0, randomCircle.y);
            float randomZ = Random.Range(-25f, 25f);
            rotation = Quaternion.Euler(90, 0, randomZ);
        }
        else
        {
            spawnPos = pos + new Vector3(0, 2f, 0);
        }

        GameObject floatText = Instantiate(floatingTextPrefab, spawnPos, rotation);
        float scale = 1f + (currentCombo * scalePerHit);
        if (isFeverMode) scale *= 3.0f;

        FloatingScore script = floatText.GetComponent<FloatingScore>();
        if (script != null)
        {
            script.Setup(score, scale, isFeverMode);
        }
    }

    void TriggerVibration()
    {
        // Solo vibramos si las opciones lo permiten
        if (OptionsManager.Instance != null && !OptionsManager.Instance.VibrationEnabled) return;

        long duration = baseVibration + (currentCombo * vibrationStep);
        if (duration > maxVibration) duration = maxVibration;
        // Vibration.Vibrate(duration); // Descomenta si usas el plugin de vibración
    }

    void ResetCombo()
    {
        currentCombo = 0;
        isFeverMode = false;
    }

    void AddShake(float amount)
    {
        if (mainCamera != null)
            mainCamera.transform.position += Random.insideUnitSphere * amount;
    }
}