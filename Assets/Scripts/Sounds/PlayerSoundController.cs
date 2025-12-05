using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : MonoBehaviour
{
    [Header("Sound Clips")]
    public AudioClip walk;
    public AudioClip wallCollision;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
    }

    public void PlayWalk()
    {
        source.PlayOneShot(walk);
    }

    public void PlayWallCollision()
    {
        source.PlayOneShot(wallCollision);
    }
}
