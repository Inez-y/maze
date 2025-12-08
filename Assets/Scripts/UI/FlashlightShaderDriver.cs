using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightShaderDriver : MonoBehaviour
{
    [Header("All materials using Custom/DayNightFogFlashlightGlobal")]
    public Material[] worldMaterials;   // all wall/floor materials here

    [Header("Where the flashlight is in the world")]
    public Transform flashlightTransform;  // camera or FlashlightPivot

    [Header("Flashlight tuning (applied to ALL materials)")]
    public float flashIntensity = 2f;    // global brightness
    public float flashRange     = 20f;
    public float flashInnerDeg  = 15f;
    public float flashOuterDeg  = 30f;

    private bool flashlightOn = false;

    // Property IDs
    private static readonly int FlashOnID        = Shader.PropertyToID("_FlashOn");
    private static readonly int FlashPosID       = Shader.PropertyToID("_FlashPos");
    private static readonly int FlashDirID       = Shader.PropertyToID("_FlashDir");
    private static readonly int FlashIntensityID = Shader.PropertyToID("_FlashIntensity");
    private static readonly int FlashRangeID     = Shader.PropertyToID("_FlashRange");
    private static readonly int FlashInnerID     = Shader.PropertyToID("_FlashInnerAngle");
    private static readonly int FlashOuterID     = Shader.PropertyToID("_FlashOuterAngle");

    private void Start()
    {
        // start off
        SetFlashOn(0f);
        ApplyStaticParams();
    }

    private void Update()
    {
        if (flashlightTransform == null || Keyboard.current == null)
            return;

        // toggle
        if (Keyboard.current.periodKey.wasPressedThisFrame)
        {
            flashlightOn = !flashlightOn;
            SetFlashOn(flashlightOn ? 1f : 0f);
        }

        // update cone params every frame (so changing in inspector at runtime works)
        ApplyStaticParams();

        // update position/direction
        Vector3 pos = flashlightTransform.position;
        Vector3 dir = flashlightTransform.forward;

        for (int i = 0; i < worldMaterials.Length; i++)
        {
            var m = worldMaterials[i];
            if (m == null) continue;

            m.SetVector(FlashPosID, pos);
            m.SetVector(FlashDirID, dir);
        }
    }

    private void SetFlashOn(float value)
    {
        for (int i = 0; i < worldMaterials.Length; i++)
        {
            var m = worldMaterials[i];
            if (m == null) continue;

            m.SetFloat(FlashOnID, value);
        }
    }

    private void ApplyStaticParams()
    {
        for (int i = 0; i < worldMaterials.Length; i++)
        {
            var m = worldMaterials[i];
            if (m == null) continue;

            m.SetFloat(FlashIntensityID, flashIntensity);
            m.SetFloat(FlashRangeID,     flashRange);
            m.SetFloat(FlashInnerID,     flashInnerDeg);
            m.SetFloat(FlashOuterID,     flashOuterDeg);
        }
    }
}
