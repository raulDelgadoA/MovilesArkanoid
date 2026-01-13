using UnityEngine;
using TMPro;

public class FloatingScore : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;
    public float lifeTime = 1.2f; // Duran un pelín más para lucirse
    public Vector3 motionDirection = Vector3.up;

    [Header("Animación Pop")]
    public float popDuration = 0.4f; // Cuánto tarda en crecer
    public AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.7f, 1.2f), // Overshoot (crece más de la cuenta)
        new Keyframe(1, 1)        // Se estabiliza
    );

    private TextMeshPro textMesh;
    private float timer;
    private Vector3 targetScale;
    private bool isFever;
    private Color startColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(int score, float scale, bool _isFever)
    {
        if (textMesh == null) return;

        isFever = _isFever;
        textMesh.text = "+" + score;

        // Guardamos el tamaño objetivo, pero empezamos en 0 para hacer el POP
        targetScale = Vector3.one * scale;
        transform.localScale = Vector3.zero;

        if (isFever)
        {
            // En modo fiebre, empezamos Amarillo/Dorado
            textMesh.color = new Color(1f, 0.8f, 0f);
            textMesh.fontStyle = FontStyles.Bold;
            // Los números gigantes suben MÁS LENTO para dar sensación de "peso" y grandeza
            moveSpeed = 1.5f;
        }
        else
        {
            textMesh.color = Color.white;
        }

        startColor = textMesh.color;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1. ANIMACIÓN DE ENTRADA (POP ELÁSTICO)
        if (timer <= popDuration)
        {
            float progress = timer / popDuration;
            float curveValue = popCurve.Evaluate(progress);
            transform.localScale = targetScale * curveValue;
        }
        else
        {
            // Vibración suave continua
            if (isFever)
            {
                float shake = Mathf.Sin(Time.time * 20f) * 0.1f * targetScale.x;
                transform.localScale = targetScale + (Vector3.one * shake);
            }
        }

        // 2. MOVIMIENTO
        transform.position += transform.up * moveSpeed * Time.deltaTime;
        // Nota: Usamos 'transform.up' local porque ya rotamos el objeto 90 grados en el Manager.

        // 3. EFECTO ARCOÍRIS (Solo Modo Fiebre) - ESTILO BALATRO
        if (isFever && textMesh != null)
        {
            float hue = Mathf.PingPong(Time.time * 2f, 1f); // Ciclo de color rápido
            // Mantenemos la saturación y brillo altos
            Color rainbow = Color.HSVToRGB(hue, 0.7f, 1f);
            // Mezclamos un poco con el dorado original para que no pierda brillo
            textMesh.color = Color.Lerp(startColor, rainbow, 0.5f);
        }

        // 4. DESVANECER (Fade Out) al final
        if (timer > lifeTime * 0.6f)
        {
            float alphaDuration = lifeTime - (lifeTime * 0.6f);
            float alphaTimer = timer - (lifeTime * 0.6f);
            float alpha = 1f - (alphaTimer / alphaDuration);

            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = alpha;
                textMesh.color = c;
            }
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}