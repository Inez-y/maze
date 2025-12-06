using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightSeparateController : MonoBehaviour
{
    [Header("Material using 'Custom/FlashlightOnly'")]
    public Material flashlightMaterial;

    [Header("Camera that holds the flashlight")]
    public Camera flashlightCamera;   // usually main camera

    private bool flashlightOn = false;

    // Property names in the shader
    private static readonly int FlashOnID   = Shader.PropertyToID("_FlashOn");
    private static readonly int FlashPosID  = Shader.PropertyToID("_FlashPos");
    private static readonly int FlashDirID  = Shader.PropertyToID("_FlashDir");

    private void Start()
    {
        if (flashlightCamera == null)
            flashlightCamera = Camera.main;

        if (flashlightMaterial != null)
        {
            // Start with flashlight off
            flashlightMaterial.SetFloat(FlashOnID, 0f);
        }
    }

    private void Update()
    {
        if (flashlightMaterial == null || flashlightCamera == null)
            return;

        if (Keyboard.current == null) return;

        // "." key toggle
        if (Keyboard.current.periodKey.wasPressedThisFrame)
        {
            flashlightOn = !flashlightOn;
            flashlightMaterial.SetFloat(FlashOnID, flashlightOn ? 1f : 0f);
        }

        // Always update flashlight position / direction
        Vector3 pos = flashlightCamera.transform.position;
        Vector3 dir = flashlightCamera.transform.forward;

        flashlightMaterial.SetVector(FlashPosID, pos);
        flashlightMaterial.SetVector(FlashDirID, dir);
    }
}
