using UnityEngine;
using UnityEngine.InputSystem;

public enum PaddleType
{
    None,
    Player,
    AI
}

public class Paddle : MonoBehaviour
{
    public PaddleType paddleType = PaddleType.None;
    public Ball ball;

    public float speed = 10f;
    private float moveInput;
    private Rigidbody rb;

    public void Awake()
    {
        rb = GetComponent<Rigidbody>();
        EventManager.GameStart += SetPaddleType;
    }

    public void SetPaddleType(PaddleType pt)
    {
        if (gameObject.tag == "P0")
            paddleType = PaddleType.Player;
        else if (gameObject.tag == "P1")
            paddleType = pt;
    }

    public void OnMove(InputValue value)
    {
        if (paddleType == PaddleType.Player)
        {
            moveInput = value.Get<float>();
        }
    }

    public void FixedUpdate()
    {
        if (paddleType == PaddleType.Player)
        {
            rb.linearVelocity = new Vector3(0f, moveInput * speed, 0f);
        }
        else if (paddleType == PaddleType.AI)
        {
            ExecuteAIMove();
        }
    }

    private void ExecuteAIMove()
    {
        if (ball == null) return;

        float dy = ball.transform.position.y - rb.position.y;

        // Deadzone to prevent jitter
        if (Mathf.Abs(dy) < 0.05f)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Move toward ball, clamped by speed
        float vy = Mathf.Clamp(dy * 5f, -speed, speed);
        rb.linearVelocity = new Vector3(0f, vy, 0f);
    }
    
    public void OnDestroy()
    {
        EventManager.GameStart -= SetPaddleType;
    }
}
