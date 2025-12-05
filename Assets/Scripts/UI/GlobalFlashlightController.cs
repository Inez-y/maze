using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalFlashlightController : MonoBehaviour
{
    private const string FLASH_ON_PROP  = "_GlobalFlashlightOn";
    private const string FLASH_POS_PROP = "_FlashlightPos";
    private const string FLASH_DIR_PROP = "_FlashlightDir";

    [Header("Camera for flashlight")]
    public Camera flashlightCamera;   // usually your main camera

    private bool flashlightOn = false;

    private void Start()
    {
        if (flashlightCamera == null)
            flashlightCamera = Camera.main;

        // Start with flashlight off
        flashlightOn = false;
        Shader.SetGlobalFloat(FLASH_ON_PROP, 0f);

        // Initialize position/direction
        UpdateFlashlightTransform();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // . (period) key toggle
        if (Keyboard.current.periodKey.wasPressedThisFrame)
        {
            flashlightOn = !flashlightOn;
            Shader.SetGlobalFloat(FLASH_ON_PROP, flashlightOn ? 1f : 0f);
        }

        // Always keep flashlight at camera position & direction
        UpdateFlashlightTransform();
    }

    private void UpdateFlashlightTransform()
    {
        if (flashlightCamera == null) return;

        Vector3 pos = flashlightCamera.transform.position;
        Vector3 dir = flashlightCamera.transform.forward;

        Shader.SetGlobalVector(FLASH_POS_PROP, pos);
        Shader.SetGlobalVector(FLASH_DIR_PROP, dir);
    }
}
