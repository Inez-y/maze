using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemySoundController : MonoBehaviour
{
    [Header("Sound Clips")]
    public AudioClip deathSFX;
    public AudioClip respawnSFX;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    public void PlayDeath()
    {
        if (deathSFX != null)
            source.PlayOneShot(deathSFX);
    }

    public void PlayRespawn()
    {
        if (respawnSFX != null)
            source.PlayOneShot(respawnSFX);
    }
}
