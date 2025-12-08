using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Light))]
public class SimpleFlashlightController : MonoBehaviour
{
    private Light flashlightLight;
    private bool flashlightOn = false;

    private void Awake()
    {
        flashlightLight = GetComponent<Light>();
        flashlightLight.enabled = false; // start off
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // "." key toggle, same as your current script
        if (Keyboard.current.periodKey.wasPressedThisFrame)
        {
            flashlightOn = !flashlightOn;
            flashlightLight.enabled = flashlightOn;
        }
    }
}
