using UnityEngine;

// IMPORTANTE: Requiere que el objeto tenga el script BrickController también
[RequireComponent(typeof(BrickController))]
public class BossProjectile : MonoBehaviour
{
    public float speed = 7f;

    [Range(0, 1)]
    public float powerUpChance = 0.3f; // 30% de probabilidad de traer regalo

    void Start()
    {
        // 1. Configuramos el Power-Up aleatorio al nacer
        BrickController brick = GetComponent<BrickController>();

        if (brick != null)
        {
            // Tiramos el dado
            if (Random.value < powerUpChance)
            {
                // Asignamos un powerup aleatorio (del 1 al 4)
                PowerUpType randomType = (PowerUpType)Random.Range(1, 5);
                brick.SetupPowerUp(randomType);
            }
            else
            {
                // Si no tiene powerup, le ponemos color rojo peligroso
                // (O el color que tú quieras para diferenciarlo)
                Renderer rend = GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = Color.red;
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", Color.red * 2f);
                }
            }
        }
    }

    void Update()
    {
        // Movimiento hacia abajo (World Space para evitar líos de rotación)
        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);

        // Si se pasa de largo, se autodestruye
        if (transform.position.z < -15f)
        {
            Destroy(gameObject);
        }
    }

    // Usamos OnCollisionEnter porque ya NO es un Trigger
    void OnCollisionEnter(Collision collision)
    {
        // 1. Si choca con la PALA -> Daño y castigo
        if (collision.gameObject.CompareTag("Paddle"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(-250); // Quitar puntos
                // Aquí podrías añadir sonido de error/daño
            }

            Destroy(gameObject); // El proyectil desaparece al impactar
        }

        // 2. Si choca con la DEATH ZONE (Suelo) -> Desaparece sin más
        else if (collision.gameObject.CompareTag("DeathZone"))
        {
            Destroy(gameObject);
        }

        // NOTA: No necesitamos comprobar "Ball" aquí.
        // Como el objeto tiene el Tag "Brick", el BallController se encarga
        // automáticamente de rebotar, destruirlo y soltar el PowerUp.
    }
}