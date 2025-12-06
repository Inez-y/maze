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

        // start with whatever useNightBGM currently is
        UpdateClip();
    }

    public void SetNightMode(bool night)
    {
        if (useNightBGM == night) return; // already in this mode

        useNightBGM = night;
        UpdateClip();
    }

    private void UpdateClip()
    {
        AudioClip newClip = useNightBGM ? nightBGM : dayBGM;
        if (newClip == null) return;

        bool wasPlaying = source.isPlaying;

        source.Stop();
        source.clip = newClip;

        if (wasPlaying)
            source.Play();
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
