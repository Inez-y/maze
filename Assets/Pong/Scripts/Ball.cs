using UnityEngine;

public class Ball : MonoBehaviour
{
    public float startSpeed = 5f;
    public float acceleration = 0.5f;

    private float speed;
    private Vector3 dir;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        gameObject.SetActive(false);
        EventManager.GameStart += OnGameStart;
        EventManager.GameEnd += OnGameEnd;
    }

    void OnGameStart(PaddleType pt)
    {
        gameObject.SetActive(true);
        ResetBall();
    }

    void OnGameEnd(int winner)
    {
        gameObject.SetActive(false);
    }

    void FixedUpdate()
    {
        speed += acceleration * Time.fixedDeltaTime;
        rb.linearVelocity = dir * speed;
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.gameObject.CompareTag("W0"))
        {
            EventManager.TriggerPointScored(0);
            ResetBall();
        }
        else if (c.gameObject.CompareTag("W1"))
        {
            EventManager.TriggerPointScored(1);
            ResetBall();
        }
        else
        {
            dir = Vector3.Reflect(dir, c.contacts[0].normal).normalized;
            rb.linearVelocity = dir * speed;
        }
    }

    void ResetBall()
    {
        transform.position = Vector3.zero;
        dir = GetRandomDirection();
        speed = startSpeed;
        rb.linearVelocity = dir * speed;
    }

    Vector3 GetRandomDirection()
    {
        Vector3 d;
        do
        {
            d = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), 0f).normalized;
        } 
        while (Mathf.Abs(d.x) < 0.4f || Mathf.Abs(d.y) > 0.8f);
        return d;
    }

    void OnDestroy()
    {
        EventManager.GameStart -= OnGameStart;
        EventManager.GameEnd -= OnGameEnd;
    }
}
