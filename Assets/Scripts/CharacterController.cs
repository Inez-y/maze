using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FPPlayerController : MonoBehaviour
{
    [Header("Tuning")]
    public float moveSpeed = 4f;
    public float mouseSensitivity = 100f; // overall look scale
    public bool invertY = false;
    public float maxPitch = 80f;

    [Header("Scene Refs")]
    public Transform cam; // if not set, will auto-find a child Camera

    [Header("Action Names (PlayerInput)")]
    public string moveActionName   = "Move";        // Vector2
    public string lookActionName   = "Look";        // Vector2
    public string noclipActionName = "NoclipToggle";// Button
    public string resetActionName  = "Reset";       // Button

    CharacterController cc;
    PlayerInput playerInput;
    InputAction moveAction, lookAction, noclipAction, resetAction;

    Vector3 startPos;
    Quaternion startRot;
    float startPitch;

    float pitch;
    bool noclip;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Ensure camera reference
        if (cam == null)
        {
            var camComp = GetComponentInChildren<Camera>(true);
            if (camComp != null) cam = camComp.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        startPos   = transform.position;
        startRot   = transform.rotation;
        startPitch = 0f;

        var map = playerInput.actions;
        moveAction   = map.FindAction(moveActionName,   false);
        lookAction   = map.FindAction(lookActionName,   false);
        noclipAction = map.FindAction(noclipActionName, false);
        resetAction  = map.FindAction(resetActionName,  false);

        if (noclipAction != null) noclipAction.performed += _ => ToggleNoclip();
        if (resetAction  != null) resetAction.performed  += _ => ResetToStart();

#if UNITY_EDITOR
        if (cam == null) Debug.LogError("FPPlayerController: No Camera assigned/found. Pitch cannot be applied.");
        if (lookAction == null) Debug.LogError($"FPPlayerController: Look action '{lookActionName}' not found.");
        if (moveAction == null) Debug.LogError($"FPPlayerController: Move action '{moveActionName}' not found.");
#endif
    }

    void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        noclipAction?.Enable();
        resetAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        noclipAction?.Disable();
        resetAction?.Disable();
    }

    void Update()
    {
        // -------- Look (mouse + right stick) --------
        Vector2 look = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        if (invertY) look.y = -look.y;

        // Mouse: pixels/frame, Stick: -1..1 — both scaled by sensitivity & dt
        Vector2 scaled = look * mouseSensitivity * Time.deltaTime;

        // Apply pitch to the camera (child), yaw to the player (root)
        pitch = Mathf.Clamp(pitch - scaled.y, -maxPitch, maxPitch);
        if (cam != null)
        {
            cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        transform.Rotate(0f, scaled.x, 0f);

        // -------- Move (WASD + left stick) --------
        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 dir = transform.forward * move.y + transform.right * move.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (noclip)
        {
            transform.position += dir * moveSpeed * Time.deltaTime;
        }
        else
        {
            cc.SimpleMove(dir * moveSpeed);
        }
    }

    void ToggleNoclip()
    {
        noclip = !noclip;
        if (cc != null) cc.detectCollisions = !noclip;
    }

    void ResetToStart()
    {
        bool wasEnabled = cc.enabled;
        cc.enabled = false;

        transform.SetPositionAndRotation(startPos, startRot);
        pitch = startPitch;
        if (cam != null) cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        cc.enabled = wasEnabled;
    }
}
