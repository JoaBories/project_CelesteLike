using UnityEngine;

public class Spring : MonoBehaviour
{
    [SerializeField] private float springForce;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("spring"))
        {
            rb.velocity = Vector2.zero;
            PlayerMovements.instance.spring();
            rb.AddForce(Vector2.up * springForce, ForceMode2D.Impulse);
        }
    }
}
