using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D m_rigidbody2D;
    private Gatherinput m_gatherinput;
    private Transform m_transform;
    [SerializeField] private float speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_rigidbody2D = GetComponent<Rigidbody2D>();
        m_gatherinput = GetComponent<Gatherinput>();
        m_transform = GetComponent<Transform>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_rigidbody2D.linearVelocity = new Vector2( speed * m_gatherinput.ValueX, m_rigidbody2D.linearVelocityY);
    }
}
