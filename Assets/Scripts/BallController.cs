using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class BallController : MonoBehaviour
{
    [Header("Sound Clips")]
    public AudioClip bounceSFX;     
    public AudioClip enemyHitSFX;   

    [Header("Score")]
    public int scoreValue = 1;     

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void Start()
    {
        Destroy(gameObject, 1f);   
    }

    void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;

        // 1) Hit enemy → sound, score, destroy immediately
        if (other.CompareTag("Enemy"))
        {
            if (enemyHitSFX != null)
                AudioSource.PlayClipAtPoint(enemyHitSFX, transform.position);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(scoreValue);
                Debug.Log("Score is now: " + ScoreManager.Instance.Score);
            }
            else
            {
                Debug.LogWarning("ScoreManager.Instance is null!");
            }

            Destroy(gameObject);    
            Debug.Log("Ball hits the enemy!!");
            return;
        }

        // 2) Hit wall or floor → bounce sound
        if (other.CompareTag("Wall") || other.CompareTag("Floor"))
        {
            Debug.Log("Ball hits wall or floor!");
            if (bounceSFX != null)
                AudioSource.PlayClipAtPoint(bounceSFX, transform.position);
        }
    }
}
