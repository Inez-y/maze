

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class EnemyControllerFSM : MonoBehaviour
{
    [Header("Refs")]
    public MazeGenerator maze;
    public Animator animator;

    [Header("Movement")]
    public float walkSpeed = 2.2f;
    public float runSpeed  = 4.0f;
    public float waitAtCell = 0.4f;
    public float yOffset = 0f;
    public float gravity = -9.81f;
    public float arriveEpsilon = 0.03f;

    [Header("Animation (BlendTree thresholds: 0=Idle, 1=Walk, 2=Run)")]
    public float animWalkValue = 1f;
    public float animRunValue  = 2f;

    [Header("Anti-stuck")]
    public float minProgress = 0.02f;   // meters per check window
    public float stuckWindow = 0.35f;   // seconds before we consider it stuck
    public float wallPush    = 0.6f;    // push off wall when hitting sides

    enum State { Idle, Choose, Move }
    State state;
    float stateTimer;

    CharacterController cc;
    Vector3 velY;          // vertical velocity
    float stuckTimer;
    Vector3 lastCheckPos;
    int sideHitFrames;

    int cx, cy, prevCx, prevCy;
    Vector3 targetPos;
    float currentSpeed;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        if (!maze) { enabled = false; return; }

        cx = maze.width - 1;
        cy = maze.height - 1;
        transform.position = maze.CellCenter(cx, cy) + Vector3.up * yOffset;

        cc.stepOffset = 0.3f;
        cc.skinWidth  = 0.02f;

        lastCheckPos = transform.position;
        stuckTimer   = 0f;
        sideHitFrames = 0;

        // start idle
        if (animator) animator.SetFloat(SpeedHash, 0f);
        Switch(State.Idle, waitAtCell);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // small horizontal nudge away from wall faces
        Vector3 n = hit.normal; n.y = 0f;
        if (n.sqrMagnitude > 0.0001f)
            cc.Move(n.normalized * wallPush * Time.deltaTime);
    }

    void Update()
    {
        // gravity
        if (cc.isGrounded && velY.y < 0f) velY.y = -2f;
        velY.y += gravity * Time.deltaTime;

        switch (state)
        {
            case State.Idle:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) Switch(State.Choose);
                cc.Move(velY * Time.deltaTime);
                break;

            case State.Choose:
            {
                var neighbors = new List<(int,int)>(maze.OpenNeighbors(cx, cy).Select(n => (n.nx, n.ny)));
                if (neighbors.Count > 1)
                    neighbors.RemoveAll(n => n.Item1 == prevCx && n.Item2 == prevCy);

                if (neighbors.Count == 0)
                {
                    // dead end → step back if possible
                    if (prevCx != cx || prevCy != cy) neighbors.Add((prevCx, prevCy));
                    else { Switch(State.Idle, waitAtCell); break; }
                }

                var next = neighbors[Random.Range(0, neighbors.Count)];
                prevCx = cx; prevCy = cy;
                cx = next.Item1; cy = next.Item2;

                targetPos = maze.CellCenter(cx, cy) + Vector3.up * yOffset;

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

                // face direction smoothly
                if (horizontal.sqrMagnitude > 0.0001f)
                {
                    var look = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
                }

                // collide-aware move
                Vector3 step = horizontal.normalized * currentSpeed * Time.deltaTime;
                var flags = cc.Move(step + velY * Time.deltaTime);

                // count side hits (scraping walls)
                if ((flags & CollisionFlags.Sides) != 0) sideHitFrames++;
                else sideHitFrames = 0;

                // arrived?
                if (horizontal.sqrMagnitude <= arriveEpsilon * arriveEpsilon)
                {
                    if (animator) animator.SetFloat(SpeedHash, 0f);
                    Switch(State.Idle, waitAtCell);
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
                        // snap back toward current cell center (keeps on-grid)
                        Vector3 center = maze.CellCenter(cx, cy) + Vector3.up * yOffset;
                        transform.position = Vector3.Lerp(transform.position, center, 0.6f);

                        if (animator) animator.SetFloat(SpeedHash, 0f);
                        Switch(State.Idle, 0.05f); // quick rethink
                        sideHitFrames = 0;
                    }
                }
                break;
            }
        }
    }

    void Switch(State s, float timer = 0f)
    {
        state = s;
        stateTimer = timer;
    }

    void OnDisable()
    {
        if (animator) animator.SetFloat(SpeedHash, 0f);
    }
}
