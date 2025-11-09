using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FPPlayerController : MonoBehaviour
{
    [Header("Tuning")]
    public float moveSpeed = 4f;

    [Header("Scene Refs")]
    public Transform cam;              // Assign the Cinemachine camera (or it will auto-find)

    [Header("Options")]
    public bool alignYawToCamera = true;  // Rotate player Y to match camera Y each frame

    [Header("Action Names (PlayerInput)")]
    public string moveActionName   = "Move";         // Vector2
    public string noclipActionName = "NoclipToggle"; // Button
    public string resetActionName  = "Reset";        // Button

    CharacterController cc;
    PlayerInput playerInput;
    InputAction moveAction, noclipAction, resetAction;

    Vector3 startPos;
    Quaternion startRot;
    bool noclip;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (cam == null)
        {
            var camComp = GetComponentInChildren<Camera>(true);
            if (camComp != null) cam = camComp.transform;
            else if (Camera.main != null) cam = Camera.main.transform;

        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        startPos = transform.position;
        startRot = transform.rotation;

        var map = playerInput.actions;
        moveAction   = map.FindAction(moveActionName,   false);
        noclipAction = map.FindAction(noclipActionName, false);
        resetAction  = map.FindAction(resetActionName,  false);

        if (noclipAction != null) noclipAction.performed += _ => ToggleNoclip();
        if (resetAction  != null) resetAction.performed  += _ => ResetToStart();

#if UNITY_EDITOR
        if (moveAction == null) Debug.LogError($"FPPlayerController: Move action '{moveActionName}' not found.");
        if (cam == null) Debug.LogWarning("FPPlayerController: No camera reference; movement will fall back to player forward.");
#endif
    }

    void OnEnable()
    {
        moveAction?.Enable();
        noclipAction?.Enable();
        resetAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        noclipAction?.Disable();
        resetAction?.Disable();
    }

    void Update()
    {
        // Optionally rotate the player’s Yaw to the camera’s Yaw so the model faces where we look.
        if (alignYawToCamera && cam != null)
        {
            Vector3 e = cam.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }

        // Read input
        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        // Build movement direction in camera space (flattened), so forward = where the camera looks.
        Vector3 fwd, right;
        if (cam != null)
        {
            fwd = cam.forward;  fwd.y = 0f;  fwd.Normalize();
            right = cam.right;  right.y = 0f; right.Normalize();
        }
        else
        {
            // Fallback to player orientation
            fwd = transform.forward;
            right = transform.right;
        }

        Vector3 dir = fwd * move.y + right * move.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (noclip)
            transform.position += dir * moveSpeed * Time.deltaTime;
        else
            cc.SimpleMove(dir * moveSpeed);


        // camera reset
        if (resetAction != null) resetAction.performed += _ => ResetToStart();

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

        // Reset player body position & rotation
        transform.SetPositionAndRotation(startPos, startRot);

        // Reset camera to its original local orientation
        if (cam != null)
        {
            cam.localPosition = new Vector3(2.65f,0.75f,0.3350065f);             
            cam.localRotation = Quaternion.identity;   // gives 0,0,0  
        }

        cc.enabled = wasEnabled;
    }

}
