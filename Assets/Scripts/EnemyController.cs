using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class EnemyControllerFSM : MonoBehaviour
{
    [Header("Refs")]
    public FixedMap maze;
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 2.2f;
    public float runSpeed = 4.0f;
    public float waitAtCell = 0.4f;
    public float yOffset = 0f;
    public float gravity = -9.81f;
    public float arriveEpsilon = 0.03f;
    const float IdleDuration = 1f;   


    [Header("Animation (BlendTree thresholds: 0=Idle, 1=Walk, 2=Run)")]
    public float animWalkValue = 1f;
    public float animRunValue = 2f;

    [Header("Anti-stuck")]
    public float minProgress = 0.22f;  // meters per check window
    public float stuckWindow = 0.25f;  // seconds between progress checks
    public float wallPushStrength = 0.9f;  // m/s sideways push off walls
    public float recenterSpeed = 3.0f;   // m/s toward cell center

    enum State { Idle, Choose, Move, Recenter }
    State state;
    float stateTimer;

    CharacterController cc;
    Vector3 velY;              
    float stuckTimer;
    Vector3 lastCheckPos;
    int sideHitFrames;

    int cx, cy, prevCx, prevCy; // current/previous grid cell
    Vector3 targetPos;
    float currentSpeed;

    Vector3 wallPushAccum; // collected in collision callback, applied next frame

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        // Ensure Animator drives only pose, not motion
        if (animator) animator.applyRootMotion = false;
    }

    Vector3 ClampInsideInnerBounds(Vector3 pos)
    {
        var ib = maze.GetInnerBounds();
        // keep a margin for the CharacterController radius
        float m = cc ? cc.radius : 0.5f;
        pos.x = Mathf.Clamp(pos.x, ib.min.x + m, ib.max.x - m);
        pos.z = Mathf.Clamp(pos.z, ib.min.z + m, ib.max.z - m);
        return pos;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Accumulate horizontal push; DO NOT call cc.Move here
        var n = hit.normal; n.y = 0f;
        if (n.sqrMagnitude > 0.0001f)
            wallPushAccum += n.normalized;
    }

    void Update()
    {
        if (!cc) return;

        // gravity
        if (cc.isGrounded && velY.y < 0f) velY.y = -2f;
        velY.y += gravity * Time.deltaTime;

        // normalized wall push for this frame
        Vector3 wallPush = Vector3.zero;
        if (wallPushAccum.sqrMagnitude > 0.0001f)
        {
            wallPush = wallPushAccum.normalized * wallPushStrength * Time.deltaTime;
            wallPushAccum = Vector3.zero;
        }

        switch (state)
        {
            case State.Idle:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) Switch(State.Choose);
                cc.Move(velY * Time.deltaTime + wallPush);
                break;

            case State.Choose:
                {
                    var neighbors = new List<(int, int)>(maze.OpenNeighbors(cx, cy).Select(n => (n.nx, n.ny)));

                    // avoid immediate backtrack when there are options
                    if (neighbors.Count > 1)
                        neighbors.RemoveAll(n => n.Item1 == prevCx && n.Item2 == prevCy);

                    if (neighbors.Count == 0)
                    {
                        // dead end → step back if possible
                        if (prevCx != cx || prevCy != cy) neighbors.Add((prevCx, prevCy));
                        else { Switch(State.Idle, IdleDuration); break; }
                    }

                    var next = neighbors[Random.Range(0, neighbors.Count)];
                    prevCx = cx; prevCy = cy;
                    cx = next.Item1; cy = next.Item2;

                    // targetPos = maze.CellCenter(cx, cy) + Vector3.up * yOffset;
                    targetPos = maze.CellCenter(cx, cy) + Vector3.up * yOffset;
                    targetPos = ClampInsideInnerBounds(targetPos);   // clamp targets too

                    // randomly walk or run
                    bool doRun = Random.value < 0.5f;
                    currentSpeed = doRun ? runSpeed : walkSpeed;
                    if (animator) animator.SetFloat(SpeedHash, doRun ? animRunValue : animWalkValue);

                    Switch(State.Move);
                    break;
                }

            case State.Move:
                {
                    var to = targetPos - transform.position;
                    Vector3 horizontal = new Vector3(to.x, 0f, to.z);

                    // Debug.Log($"[Enemy] dist={horizontal.magnitude:0.000} speed={currentSpeed:0.0}");
                    // face direction smoothly
                    if (horizontal.sqrMagnitude > 0.0001f)
                    {
                        var look = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
                        transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
                    }

                    // move with collisions
                    Vector3 step = horizontal.normalized * currentSpeed * Time.deltaTime;
                    var flags = cc.Move(step + velY * Time.deltaTime + wallPush);

                    // side hits counting (scraping)
                    if ((flags & CollisionFlags.Sides) != 0) sideHitFrames++;
                    else sideHitFrames = 0;

                    // arrived?
                    if (horizontal.sqrMagnitude <= arriveEpsilon * arriveEpsilon)
                    {
                        if (animator) animator.SetFloat(SpeedHash, 0f);
                        Switch(State.Idle, IdleDuration);
                        stuckTimer = 0f;
                        lastCheckPos = transform.position;
                        sideHitFrames = 0;
                        break;
                    }

                    // stuck detection (no progress OR scraping too long)
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= stuckWindow)
                    {
                        float progressed = Vector3.Distance(transform.position, lastCheckPos);
                        lastCheckPos = transform.position;
                        stuckTimer = 0f;

                        bool scraping = sideHitFrames > 6;
                        if (progressed < minProgress || scraping)
                        {
                            // smooth recentre (no teleport)
                            if (animator) animator.SetFloat(SpeedHash, 0f);
                            Switch(State.Recenter, 0.15f);
                        }
                    }
                    break;
                }

            case State.Recenter:
                {
                    Vector3 center = maze.CellCenter(cx, cy) + Vector3.up * yOffset;
                    Vector3 toCenter = center - transform.position;
                    Vector3 horiz = new Vector3(toCenter.x, 0f, toCenter.z);

                    if (horiz.sqrMagnitude > arriveEpsilon * arriveEpsilon)
                    {
                        Vector3 step = horiz.normalized * recenterSpeed * Time.deltaTime;
                        cc.Move(step + velY * Time.deltaTime + wallPush);
                        Switch(State.Idle, 1.5f);
                    }
                    else
                    {
                        // switch to idle duration, max 1 sec
                        Switch(State.Idle, IdleDuration);
                        sideHitFrames = 0;
                    }
                    break;
                }
        }
    }

    void Switch(State s, float timer = 0f)
    {
        state = s;
        stateTimer = timer;

        // OnEnter hooks
        if (state == State.Idle)
        {
            // ensure idle pose and clear anti-stuck bookkeeping
            if (animator) animator.SetFloat(SpeedHash, 0f);
            stuckTimer = 0f;
            sideHitFrames = 0;
            lastCheckPos = transform.position;
        }

        // Debug:
        Debug.Log($"[Enemy] -> {state} (t={timer:0.00})");
    }

}