using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FPPlayerController : MonoBehaviour
{
    [Header("Tuning")]
    public float moveSpeed = 4f;

    [Header("Scene Refs")]
    public Transform cam;             

    [Header("Options")]
    public bool alignYawToCamera = true;  // rotate player Y to match camera Y each frame

    [Header("Action Names (PlayerInput)")]
    public string moveActionName   = "Move";         // Vector2
    public string noclipActionName = "NoclipToggle"; // Button
    public string resetActionName  = "Reset";        // Button

    // Sound effect
    PlayerSoundController sound;
    [Header("Sound Settings")]
    public float stepInterval = 0.5f;     
    public float wallSoundCooldown = 0.5f;
    float lastStepTime;
    float lastWallSoundTime;



    CharacterController cc;
    PlayerInput playerInput;
    InputAction moveAction, noclipAction, resetAction;

    Vector3    startPos;
    Quaternion startRot;

    // Camera start transform (so Home truly resets to the exact startup look)
    Vector3    camStartLocalPos;
    Quaternion camStartLocalRot;

    bool noclip;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        sound = GetComponent<PlayerSoundController>(); 

        if (cam == null)
        {
            var camComp = GetComponentInChildren<Camera>(true);
            if (camComp != null) cam = camComp.transform;
            else if (Camera.main != null) cam = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Save starting transforms
        startPos = transform.position;
        startRot = transform.rotation;
        if (cam != null)
        {
            camStartLocalPos = cam.localPosition;
            camStartLocalRot = cam.localRotation;
        }

        // Bind input actions once
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

    void Die()
    {
        if (cc) cc.enabled = false;
        Destroy(gameObject, 1f);
        ResetToStart();
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
        // Keep player body aligned to camera yaw if desired
        if (alignYawToCamera && cam != null)
        {
            Vector3 e = cam.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }

        // Movement in camera space (flattened)
        Vector2  move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3  fwd, right;

        if (cam != null)
        {
            fwd   = cam.forward;  fwd.y   = 0f; fwd.Normalize();
            right = cam.right;    right.y = 0f; right.Normalize();
        }
        else
        {
            fwd = transform.forward;
            right = transform.right;
        }

        Vector3 dir = fwd * move.y + right * move.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        if (noclip)
        {
            transform.position += dir * moveSpeed * Time.deltaTime;
        }
        else
        {
            cc.SimpleMove(dir * moveSpeed);

            // footstep
            if (sound != null && cc.isGrounded && dir.sqrMagnitude > 0.01f)
            {
                if (Time.time - lastStepTime > stepInterval)
                {
                    sound.PlayWalk();
                    lastStepTime = Time.time;
                }
            }
        }

    }
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (sound == null) return;

        // Consider it a "wall" if the surface is mostly vertical, not floor/ceiling.
        bool isWall =
            Vector3.Dot(hit.normal, Vector3.up) < 0.5f; 

        if (isWall && Time.time - lastWallSoundTime > wallSoundCooldown)
        {
            sound.PlayWallCollision();
            lastWallSoundTime = Time.time;
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

        // Reset player body pose
        transform.SetPositionAndRotation(startPos, startRot);

        // Reset camera local pose
        if (cam != null)
        {
            cam.localPosition = camStartLocalPos;
            cam.localRotation = camStartLocalRot;

        #if CINEMACHINE
            var pov = cam.GetComponentInParent<Cinemachine.CinemachinePOV>();
            if (pov != null)
            {
                pov.m_HorizontalAxis.Value = 0f;
                pov.m_VerticalAxis.Value   = 0f;
            }
            var free = cam.GetComponentInParent<Cinemachine.CinemachineFreeLook>();
            if (free != null)
            {
                free.m_XAxis.Value = 0f; // set to your desired default if not zero
                free.m_YAxis.Value = 0.5f;
            }
        #endif
        }

        cc.enabled = wasEnabled;
    }
}
