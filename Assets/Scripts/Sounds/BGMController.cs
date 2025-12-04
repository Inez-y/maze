using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class BGMController : MonoBehaviour
{
    [Header("Sound Clips")]
    public AudioClip dayBGM;
    public AudioClip nightBGM;

    [Header("Mode")]
    public bool useNightBGM = false;  

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;

        // choose clip based on mode
        AudioClip clipToPlay = useNightBGM ? nightBGM : dayBGM;
        if (clipToPlay != null)
        {
            source.clip = clipToPlay;
            source.Play();
        }
    }

    void Update()
    {
        // [ stop
        if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            source.Stop();
        }

        // ] play (resume current clip)
        if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            if (source.clip != null && !source.isPlaying)
                source.Play();
        }
    }
}
