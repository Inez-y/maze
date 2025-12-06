using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalFogToggle : MonoBehaviour
{
    private const string GLOBAL_FOG_PROP = "_GlobalFogToggle";

    [Header("Fog Values")]
    [Range(0f, 1f)] public float fogOffValue = 0f;
    [Range(0f, 1f)] public float fogOnValue = 1f;

    [Header("Audio Settings")]
    public BGMController bgmController;    
    public float fogOnVolume = 0.5f;      
    public float fogOffVolume = 1f;        

    private bool fogOn = false;

    private void Start()
    {
        if (bgmController == null)
            bgmController = FindObjectOfType<BGMController>();

        // Start with fog off
        fogOn = false;
        Shader.SetGlobalFloat(GLOBAL_FOG_PROP, fogOffValue);

        // Ensure volume starts correct
        if (bgmController != null)
            bgmController.SetVolume(fogOffVolume);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Slash key
        if (Keyboard.current.slashKey.wasPressedThisFrame)
        {
            fogOn = !fogOn;

            // update shader
            float value = fogOn ? fogOnValue : fogOffValue;
            Shader.SetGlobalFloat(GLOBAL_FOG_PROP, value);

            // update BGM volume
            if (bgmController != null)
            {
                float newVolume = fogOn ? fogOnVolume : fogOffVolume;
                bgmController.SetVolume(newVolume);
            }
        }
    }
}
