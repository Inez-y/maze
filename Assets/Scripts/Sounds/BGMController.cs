using UnityEngine;
using UnityEngine.InputSystem;   

[RequireComponent(typeof(AudioSource))]
public class BGMController : MonoBehaviour
{
    [Header("Sound Clips")]
    public AudioClip regularBGM;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        if (regularBGM != null)
        {
            source.loop = true;           
            source.clip = regularBGM;
            source.Play();                
        }
    }

    void Update()
    {
        // [
        if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            source.Stop();
        }

        // ]
        if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            if (regularBGM != null && !source.isPlaying)
                source.Play();
        }
    }
}
