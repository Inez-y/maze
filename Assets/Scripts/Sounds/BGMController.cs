using UnityEngine;
using UnityEngine.InputSystem;   

[RequireComponent(typeof(AudioSource))]
public class BGMController : MonoBehaviour
{
    [Header("Sound Clips")]
    public AudioClip dayBGM;
    public AudioClip nightBGM;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        // need day/night logic
        if (dayBGM != null)
        {
            source.loop = true;           
            source.clip = dayBGM;
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
            if (dayBGM != null && !source.isPlaying)
                source.Play();
        }
    }
}
