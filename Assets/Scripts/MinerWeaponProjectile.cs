using UnityEngine;

/// <summary>
/// MinerWeaponProjectile — El pico/hacha que lanza el EnemyMiner.
///
/// - Rota sobre sí mismo mientras vuela (efecto de hacha giratoria).
/// - Daña al jugador al tocarlo.
/// - Se destruye contra superficies sólidas.
/// - Opcionalmente rebota una vez antes de destruirse.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MinerWeaponProjectile : MonoBehaviour
{
    [Header("Vuelo")]
    [SerializeField] private float rotationSpeed = 540f;  // grados por segundo
    [SerializeField] private float lifetime      = 3.5f;
    [SerializeField] private int   bounces       = 1;     // rebotes antes de destruirse

    [Header("Daño")]
    [SerializeField] private int damage = 2;

    [Header("Capas")]
    [SerializeField] private LayerMask groundLayer;

    [Header("FX")]
    [SerializeField] private GameObject hitFXPrefab;
    [SerializeField] private AudioClip  sfxHit;

    private Rigidbody2D _rb;
    private int         _bounceCount;
    private bool        _initialized;
    private AudioSource _audio;

    private void Awake()
    {
        _rb    = GetComponent<Rigidbody2D>();
        _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        Destroy(gameObject, lifetime);
    }

    /// <summary>Inicializa el daño desde EnemyMiner.</summary>
    public void Init(int dmg) => damage = dmg;

    private void Update()
    {
        // Rotación constante mientras vuela (hacha giratoria)
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignorar otros enemigos
        if (other.GetComponentInParent<EnemyMiner>() != null) return;

        // Buscar PlayerHealth en el objeto o en cualquier padre
        // (el collider puede estar en un hijo del jugador)
        PlayerHealth ph = other.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            // TakeDamage requiere (int damage, Vector2 enemyPosition)
            ph.TakeDamage(damage, transform.position);
            SpawnHitFX();
            Destroy(gameObject);
            return;
        }

        // Colisión con suelo/pared
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            if (_bounceCount < bounces)
            {
                _bounceCount++;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.5f,
                                                  Mathf.Abs(_rb.linearVelocity.y) * 0.4f);
            }
            else
            {
                SpawnHitFX();
                Destroy(gameObject);
            }
        }
    }

    private void SpawnHitFX()
    {
        if (hitFXPrefab != null)
        {
            var fx = Instantiate(hitFXPrefab, transform.position, Quaternion.identity);
            Destroy(fx, 1.5f);
        }
        if (sfxHit != null)
            AudioSource.PlayClipAtPoint(sfxHit, transform.position);
    }
}
