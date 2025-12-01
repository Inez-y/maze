using UnityEngine;
using System.Collections;  
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class EnemyControllerFSM : MonoBehaviour
{
    [Header("Map")]
    public FixedMap maze;

    [Header("Start")]
    public bool      useStartTransform = false;
    public Transform startTransform;
    public bool      snapToNearestCell = true;
    public bool      keepStartYaw      = true;

    [Header("Movement")]
    public float moveSpeed = 2.0f;

    [Header("Random Speed")]
    public bool  useRandomSpeed = true;
    public float minRandomSpeed = 0f;
    public float maxRandomSpeed = 2.5f;
    public float randomSpeedInterval = 2f;

    [Header("Decision")]
    [Min(1)] public int minCellsPerDecision = 2;
    [Min(1)] public int maxCellsPerDecision = 5;

    [Header("Health")]
    public int maxHP = 3; 
    int currentHP;
    bool isDead = false;
    public GameObject enemyPrefab;  // respawn enemy


    [Header("Stuck Detection")]
    [Tooltip("How often to check if position is actually changing.")]
    public float stuckCheckInterval = 0.1f;
    [Tooltip("Minimum horizontal distance that counts as 'moved' during one interval.")]
    public float minMoveDistance = 0.04f;
    [Tooltip("Number of consecutive low-movement checks before considering the agent stuck.")]
    public int   maxStuckChecks = 1;

    [Header("Wall Detection (immediate)")]
    [Tooltip("If the CharacterController hits a side, immediately pick a new direction.")]
    public bool immediateOnSideCollision = true;
    [Tooltip("Extra raycast probe ahead to catch walls before sliding.")]
    public bool useForwardProbe = true;
    [Tooltip("How far ahead to probe for a wall (in world units).")]
    public float wallProbeDistance = 0.25f;
    [Tooltip("Layers considered solid for the forward probe.")]
    public LayerMask wallMask = ~0;


    CharacterController cc;
    Animator animator;
    Animation legacyAnim;

    int speedHash = Animator.StringToHash("Speed");
    Vector3 targetWorld;
    bool hasTarget;

    // Direction commitment state
    Vector2Int currentDir = Vector2Int.zero;
    int cellsRemainingInDecision = 0;

    // Stuck detection state
    Vector3 lastPosForStuck;
    float stuckTimer = 0f;
    int   lowMoveCount = 0;

    // Recently blocked info
    Vector2Int lastBlockedDir = Vector2Int.zero;
    Vector2Int lastBlockedCell = new Vector2Int(int.MinValue, int.MinValue);

    // Cached per-frame data
    Vector3 lastVelocity = Vector3.zero;
    CollisionFlags lastCollisionFlags = CollisionFlags.None;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        animator  = GetComponentInChildren<Animator>();
        legacyAnim = GetComponentInChildren<Animation>();

        if (!maze)
            maze = MapSpawner.ActiveMap ? MapSpawner.ActiveMap : FindFirstByType<FixedMap>();
    }

    void Start()
    {
        if (useStartTransform && startTransform)
        {
            transform.SetPositionAndRotation(
                startTransform.position,
                keepStartYaw ? Quaternion.Euler(0f, startTransform.eulerAngles.y, 0f) : startTransform.rotation);
        }

        if (maze && snapToNearestCell && maze.TryWorldToCell(transform.position, out var cx, out var cy))
            transform.position = maze.CellCenterWorld(cx, cy);

        lastPosForStuck = transform.position;

        PickNewTarget();
        UpdateAnim(0f);

        if (useRandomSpeed) StartCoroutine(RandomizeSpeedRoutine());

        currentHP = maxHP;

    }
    
    IEnumerator RandomizeSpeedRoutine()
    {
        while (true)
        {
            moveSpeed = Random.Range(minRandomSpeed, maxRandomSpeed);
            yield return new WaitForSeconds(randomSpeedInterval);
        }
    }

    public void TakeDamage(int amount = 1)
    {
        if (isDead) return;

        currentHP -= amount;
        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        isDead = true;
        if (cc) cc.enabled = false;
        Destroy(gameObject, 1f);
        Invoke(nameof(RespawnSelf), 5f);
    }

    void RespawnSelf()
    {
        if (enemyPrefab != null)
            Instantiate(enemyPrefab, transform.position, transform.rotation);
    }

    void Update()
    {
        if (!maze) return;
        if (isDead) return;


        // Proactive wall probe: if something is in front of our committed direction, reroute now.
        if (useForwardProbe && currentDir != Vector2Int.zero && IsWallAhead(out _))
        {
            MarkBlockedHere(currentDir);
            BreakCommitmentAndReroute();
        }

        var to = targetWorld - transform.position;
        var dist = to.magnitude;

        if (!hasTarget || dist < 0.1f)
            PickNewTarget();

        var dir = (targetWorld - transform.position);
        dir.y = 0f;
        var step = Mathf.Min(dir.magnitude, moveSpeed * Time.deltaTime);
        var vel = dir.normalized * (step > 0f ? moveSpeed : 0f);

        if (vel.sqrMagnitude > 0f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 720f * Time.deltaTime);

        // SimpleMove returns a bool (grounded). Read collision flags from the controller afterward.
        var grounded = cc.SimpleMove(vel);
        lastCollisionFlags = cc.collisionFlags;
        lastVelocity = vel;

        UpdateAnim(vel.magnitude);

        // Immediate response to side collision while intending to move.
        if (immediateOnSideCollision && (lastCollisionFlags & CollisionFlags.Sides) != 0 && vel.sqrMagnitude > 0.0001f)
        {
            if (currentDir != Vector2Int.zero) MarkBlockedHere(currentDir);
            BreakCommitmentAndReroute();
        }

        CheckStuckAndRecover();
    }

    void CheckStuckAndRecover()
    {
        stuckTimer += Time.deltaTime;
        if (stuckTimer < stuckCheckInterval) return;

        Vector3 now = transform.position;
        Vector2 a = new Vector2(lastPosForStuck.x, lastPosForStuck.z);
        Vector2 b = new Vector2(now.x, now.z);
        float moved = Vector2.Distance(a, b);

        if (moved < minMoveDistance)
        {
            lowMoveCount++;

            if (maze && currentDir != Vector2Int.zero && maze.TryWorldToCell(transform.position, out var cx, out var cy))
            {
                lastBlockedCell = new Vector2Int(cx, cy);
                lastBlockedDir  = currentDir;
            }

            if (lowMoveCount >= maxStuckChecks)
            {
                BreakCommitmentAndReroute();
                lowMoveCount = 0;
                lastPosForStuck = now;
                stuckTimer = 0f;
                return;
            }
        }
        else
        {
            lowMoveCount = 0;
        }

        lastPosForStuck = now;
        stuckTimer = 0f;
    }

    void BreakCommitmentAndReroute()
    {
        cellsRemainingInDecision = 0;
        currentDir = Vector2Int.zero;
        PickNewTarget();
    }

    bool IsWallAhead(out RaycastHit hit)
    {
        hit = default;
        if (!cc) return false;
        if (currentDir == Vector2Int.zero) return false;

        Vector3 origin = transform.position + Vector3.up * (cc.height * 0.5f);
        Vector3 fwd = new Vector3(currentDir.x, 0f, currentDir.y).normalized;
        float dist = wallProbeDistance + cc.radius + cc.skinWidth;

        if (Physics.Raycast(origin, fwd, out hit, dist, wallMask, QueryTriggerInteraction.Ignore))
            return true;

        Vector3 top = transform.position + Vector3.up * (cc.height - cc.radius);
        Vector3 bottom = transform.position + Vector3.up * cc.radius;
        if (Physics.CapsuleCast(bottom, top, cc.radius * 0.95f, fwd, out hit, wallProbeDistance, wallMask, QueryTriggerInteraction.Ignore))
            return true;

        return false;
    }

    void MarkBlockedHere(Vector2Int dir)
    {
        if (!maze) return;
        if (maze.TryWorldToCell(transform.position, out var cx, out var cy))
        {
            lastBlockedCell = new Vector2Int(cx, cy);
            lastBlockedDir  = dir;
        }
    }

    void PickNewTarget()
    {
        if (!maze) { hasTarget = false; return; }

        int cx, cy;
        if (!maze.TryWorldToCell(transform.position, out cx, out cy))
        {
            cx = Mathf.Clamp(Mathf.RoundToInt(transform.position.x), 0, maze.width  - 1);
            cy = Mathf.Clamp(Mathf.RoundToInt(transform.position.z), 0, maze.height - 1);
        }

        // If still committed, continue in the same direction when possible.
        if (cellsRemainingInDecision > 0 && currentDir != Vector2Int.zero)
        {
            int nx = Mathf.Clamp(cx + currentDir.x, 0, maze.width  - 1);
            int ny = Mathf.Clamp(cy + currentDir.y, 0, maze.height - 1);

            if (nx != cx || ny != cy)
            {
                cellsRemainingInDecision--;
                targetWorld = maze.CellCenterWorld(nx, ny);
                hasTarget = true;
                return;
            }
            else
            {
                cellsRemainingInDecision = 0; // force new decision
            }
        }

        var dirs = new List<Vector2Int> { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        // Prefer not to immediately reverse unless necessary.
        if (currentDir != Vector2Int.zero)
            dirs.Remove(-currentDir);

        // Avoid retrying a direction that was just blocked at this cell.
        if (lastBlockedCell.x != int.MinValue && lastBlockedCell == new Vector2Int(cx, cy))
            dirs.Remove(lastBlockedDir);

        // Remove directions that would keep us in place due to borders.
        for (int i = dirs.Count - 1; i >= 0; i--)
        {
            int tx = Mathf.Clamp(cx + dirs[i].x, 0, maze.width  - 1);
            int ty = Mathf.Clamp(cy + dirs[i].y, 0, maze.height - 1);
            if (tx == cx && ty == cy)
                dirs.RemoveAt(i);
        }

        // Final safety: if a forward probe says blocked, cull that dir too.
        if (useForwardProbe)
        {
            for (int i = dirs.Count - 1; i >= 0; i--)
            {
                var d = dirs[i];
                Vector3 fwd = new Vector3(d.x, 0f, d.y).normalized;
                if (ProbeBlocked(fwd))
                    dirs.RemoveAt(i);
            }
        }

        if (dirs.Count == 0)
            dirs.AddRange(new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down });

        var ndir = dirs[Random.Range(0, dirs.Count)];
        currentDir = ndir;
        cellsRemainingInDecision = Mathf.Max(1, Random.Range(minCellsPerDecision, maxCellsPerDecision + 1)) - 1;

        int nx2 = Mathf.Clamp(cx + ndir.x, 0, maze.width  - 1);
        int ny2 = Mathf.Clamp(cy + ndir.y, 0, maze.height - 1);

        targetWorld = maze.CellCenterWorld(nx2, ny2);
        hasTarget = true;
    }

    bool ProbeBlocked(Vector3 fwd)
    {
        if (!cc) return false;
        Vector3 origin = transform.position + Vector3.up * (cc.height * 0.5f);
        float dist = wallProbeDistance + cc.radius + cc.skinWidth;
        if (Physics.Raycast(origin, fwd, dist, wallMask, QueryTriggerInteraction.Ignore))
            return true;

        Vector3 top = transform.position + Vector3.up * (cc.height - cc.radius);
        Vector3 bottom = transform.position + Vector3.up * cc.radius;
        if (Physics.CapsuleCast(bottom, top, cc.radius * 0.95f, fwd, wallProbeDistance, wallMask, QueryTriggerInteraction.Ignore))
            return true;

        return false;
    }

    void UpdateAnim(float speed)
    {
        if (animator)
        {
            if (HasParam(animator, speedHash))
                animator.SetFloat(speedHash, speed);
            return;
        }

        if (legacyAnim)
        {
            if (speed > 0.1f && legacyAnim.GetClip("Walk"))
                legacyAnim.CrossFade("Walk", 0.1f);
            else if (legacyAnim.GetClip("Idle"))
                legacyAnim.CrossFade("Idle", 0.1f);
        }
    }

    static bool HasParam(Animator anim, int hash)
    {
        foreach (var p in anim.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

    // ---------- helpers to use new APIs on 2023+ while staying compatible ----------
    static T FindFirstByType<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
#pragma warning disable CS0618
        return Object.FindObjectOfType<T>();
#pragma warning restore CS0618
#endif
    }
}
