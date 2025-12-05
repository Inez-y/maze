using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalFogToggle : MonoBehaviour
{
    private const string GLOBAL_FOG_PROP = "_GlobalFogToggle";

    [Header("Fog Values")]
    [Range(0f, 1f)] public float fogOffValue = 0f;
    [Range(0f, 1f)] public float fogOnValue = 1f;

    private bool fogOn = false;

    private void Start()
    {
        // Start with fog off
        fogOn = false;
        Shader.SetGlobalFloat(GLOBAL_FOG_PROP, fogOffValue);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Slash / question mark key
        if (Keyboard.current.slashKey.wasPressedThisFrame)
        {
            fogOn = !fogOn;

            float value = fogOn ? fogOnValue : fogOffValue;
            Shader.SetGlobalFloat(GLOBAL_FOG_PROP, value);
        }
    }
}
