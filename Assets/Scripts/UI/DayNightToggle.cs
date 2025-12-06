using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalDayNightToggle : MonoBehaviour
{
    [Header("Blend Values")]
    [Range(0f, 1f)] public float dayValue = 0f;   // day = 0
    [Range(0f, 1f)] public float nightValue = 1f; // night = 1

    [Header("Transition")]
    public bool smoothTransition = true;
    public float transitionSpeed = 2f;

    [Header("References")]
    [SerializeField] private BGMController bgmController;   // drag in Inspector

    public bool isNight = false;
    private float targetBlend;
    private float currentBlend;

    private const string GLOBAL_PROP_NAME = "_GlobalDayNight";

    private void Start()
    {
        // if you forgot to assign it, try to find one in the scene
        if (bgmController == null)
            bgmController = FindObjectOfType<BGMController>();

        isNight = false;
        targetBlend = dayValue;
        currentBlend = dayValue;

        Shader.SetGlobalFloat(GLOBAL_PROP_NAME, currentBlend);

        // make sure BGM starts in day mode
        if (bgmController != null)
            bgmController.SetNightMode(isNight);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Press Enter to toggle
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            isNight = !isNight;
            targetBlend = isNight ? nightValue : dayValue;

            if (bgmController != null)
                bgmController.SetNightMode(isNight);
        }

        if (smoothTransition)
        {
            currentBlend = Mathf.MoveTowards(
                currentBlend,
                targetBlend,
                transitionSpeed * Time.deltaTime
            );
        }
        else
        {
            currentBlend = targetBlend;
        }

        Shader.SetGlobalFloat(GLOBAL_PROP_NAME, currentBlend);
    }
}
