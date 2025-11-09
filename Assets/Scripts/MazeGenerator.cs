using UnityEngine;
using System.Collections.Generic;

public class Cell
{
    public bool visited;
    public GameObject north, south, east, west;
}

public class MazeGenerator : MonoBehaviour
{
    [Header("Size")]
    public int width = 1, height = 1;

    [Header("Refs")]
    public GameObject cellPrefab;
    public float cellSize = 3f;
    public Transform player;
    public Transform goal;

    [Header("Options")]
    public bool keepPerimeterClosed = true;

    private Cell[,] grid;
    private System.Random rng;

    [Header("Cells")]
    public bool cellsUseCenterPivot = true; // true if prefab pivot is centered
    public bool autoScaleCellToSize = true; // scale prefab footprint to cellSize at runtime

    [Header("Alignment")]
public float cellWallThickness = 0.2f;   // set to your cell wall mesh thickness



    [Header("Generated Outer Walls")]
    public GameObject outerWallPrefab;         // assign a Cube with BoxCollider
    public float wallThickness = 0.25f;        // X for W/E, Z for S/N
    public float wallHeight = 2.0f;            // Y scale
    public Transform wallsParent;              // optional parent
    public bool buildOuterWalls = true;

    // keep refs to generated walls
    GameObject wallW, wallE, wallS, wallN;


    [Header("Outer Wall Materials")]
    public Material WallEW;
    public Material WallNS;
    public float tileMeters = 1f;
    enum WallDir { West, East, South, North }


    [Header("Wall logic for enemy")]
    public bool IsReady { get; private set; }

    void Start()
    {
        IsReady = false;
        rng = new System.Random();

        BuildGrid();
        CarveMazeDFS(0, 0);

        if (keepPerimeterClosed)
            EnsurePerimeterWallsActive();

        if (buildOuterWalls)
            BuildOuterWalls();

        if (player != null)
            player.position = CellCenter(0, 0) + Vector3.up * 1f;

        var (gx, gy) = FarthestCellFrom(0, 0);
        if (goal != null)
            goal.position = CellCenter(gx, gy);

        IsReady = true;
    }

    // ---------- Origins / centers ----------
    Vector3 MazeOrigin() => transform.position; // bottom-left corner in world

    public Vector3 CellCenter(int x, int y)
    {
        float h = cellSize * 0.5f;
        return MazeOrigin() + new Vector3(x * cellSize + h, 0f, y * cellSize + h);
    }

    // ---------- Grid build ----------
    // void BuildGrid()
    // {
    //     grid = new Cell[width, height];
    //     Vector3 origin = MazeOrigin();

    //     for (int x = 0; x < width; x++)
    //     for (int y = 0; y < height; y++)
    //     {
    //         var cellGO = Instantiate(
    //             cellPrefab,
    //             origin + new Vector3(x * cellSize, 0, y * cellSize), // << use same origin as CellCenter
    //             Quaternion.identity,
    //             transform
    //         );

    //         var t = cellGO.transform;
    //         var north = FindDeepChild(t, "WallN");
    //         var south = FindDeepChild(t, "WallS");
    //         var east  = FindDeepChild(t, "WallE");
    //         var west  = FindDeepChild(t, "WallW");

    //         if (north == null || south == null || east == null || west == null)
    //             Debug.LogWarning($"[MazeGenerator] Missing wall(s) on cell ({x},{y}). Expected WallN/WallS/WallE/WallW.", cellGO);

    //         grid[x, y] = new Cell
    //         {
    //             visited = false,
    //             north = north ? north.gameObject : null,
    //             south = south ? south.gameObject : null,
    //             east  = east  ? east.gameObject  : null,
    //             west  = west  ? west.gameObject  : null
    //         };
    //     }
    // }
    void BuildGrid()
    {
        grid = new Cell[width, height];
        Vector3 origin = MazeOrigin();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = cellsUseCenterPivot
                    ? CellCenter(x, y)                              // place by center
                    : origin + new Vector3(x * cellSize, 0, y * cellSize); // place by corner

                var cellGO = Instantiate(cellPrefab, pos, Quaternion.identity, transform);

                if (autoScaleCellToSize)
                    FitCellFootprintToCellSize(cellGO);

                var t = cellGO.transform;
                var north = FindDeepChild(t, "WallN");
                var south = FindDeepChild(t, "WallS");
                var east = FindDeepChild(t, "WallE");
                var west = FindDeepChild(t, "WallW");

                if (!north || !south || !east || !west)
                    Debug.LogWarning($"[MazeGenerator] Missing wall(s) on cell ({x},{y}). Expected WallN/WallS/WallE/WallW.", cellGO);

                grid[x, y] = new Cell
                {
                    visited = false,
                    north = north ? north.gameObject : null,
                    south = south ? south.gameObject : null,
                    east = east ? east.gameObject : null,
                    west = west ? west.gameObject : null
                };
            }
    }

    void FitCellFootprintToCellSize(GameObject cellGO)
    {
        // measure XZ size using renderers under this cell
        var rends = cellGO.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        // convert world bounds to local size (approx) by removing parent lossyScale
        var parent = cellGO.transform.parent;
        Vector3 parentScale = parent ? parent.lossyScale : Vector3.one;
        float currentX = b.size.x / parentScale.x;
        float currentZ = b.size.z / parentScale.z;

        if (currentX <= 0.0001f || currentZ <= 0.0001f) return;

        float sx = cellSize / currentX;
        float sz = cellSize / currentZ;

        // keep Y scale as-is
        var local = cellGO.transform.localScale;
        cellGO.transform.localScale = new Vector3(local.x * sx, local.y, local.z * sz);
    }



    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // ---------- Carving ----------
    void CarveMazeDFS(int sx, int sy)
    {
        Stack<(int x, int y)> stack = new();
        stack.Push((sx, sy));
        grid[sx, sy].visited = true;

        while (stack.Count > 0)
        {
            var (x, y) = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(x, y);

            if (neighbors.Count == 0) { stack.Pop(); continue; }

            var (nx, ny, dir) = neighbors[rng.Next(neighbors.Count)];
            RemoveWallBetween(x, y, nx, ny, dir);
            grid[nx, ny].visited = true;
            stack.Push((nx, ny));
        }
    }

    List<(int nx, int ny, string dir)> GetUnvisitedNeighbors(int x, int y)
    {
        var list = new List<(int, int, string)>();
        if (y + 1 < height && !grid[x, y + 1].visited) list.Add((x, y + 1, "N"));
        if (y - 1 >= 0 && !grid[x, y - 1].visited) list.Add((x, y - 1, "S"));
        if (x + 1 < width && !grid[x + 1, y].visited) list.Add((x + 1, y, "E"));
        if (x - 1 >= 0 && !grid[x - 1, y].visited) list.Add((x - 1, y, "W"));
        return list;
    }

    void RemoveWallBetween(int x, int y, int nx, int ny, string dir)
    {
        if (dir == "N") { SafeSetActive(grid[x, y].north, false); SafeSetActive(grid[nx, ny].south, false); }
        if (dir == "S") { SafeSetActive(grid[x, y].south, false); SafeSetActive(grid[nx, ny].north, false); }
        if (dir == "E") { SafeSetActive(grid[x, y].east, false); SafeSetActive(grid[nx, ny].west, false); }
        if (dir == "W") { SafeSetActive(grid[x, y].west, false); SafeSetActive(grid[nx, ny].east, false); }
    }

    // ---------- Perimeter from cell walls ----------
    void EnsurePerimeterWallsActive()
    {
        for (int y = 0; y < height; y++)
        {
            ForceWallOn(grid[0, y]?.west);
            ForceWallOn(grid[width - 1, y]?.east);
        }
        for (int x = 0; x < width; x++)
        {
            ForceWallOn(grid[x, 0]?.south);
            ForceWallOn(grid[x, height - 1]?.north);
        }
    }

    void ForceWallOn(GameObject wall)
    {
        if (!wall) return;
        SafeSetActive(wall, true);
        var mr = wall.GetComponentInChildren<MeshRenderer>(true); if (mr) mr.enabled = true;
        var col = wall.GetComponentInChildren<Collider>(true); if (col) col.enabled = true;
    }

    void SafeSetActive(GameObject go, bool state)
    {
        if (go && go.activeSelf != state) go.SetActive(state);
    }

    // ---------- Path helpers ----------
    public IEnumerable<(int nx, int ny)> OpenNeighbors(int x, int y)
    {
        if (grid[x, y].north && !grid[x, y].north.activeSelf && y + 1 < height) yield return (x, y + 1);
        if (grid[x, y].south && !grid[x, y].south.activeSelf && y - 1 >= 0) yield return (x, y - 1);
        if (grid[x, y].east && !grid[x, y].east.activeSelf && x + 1 < width) yield return (x + 1, y);
        if (grid[x, y].west && !grid[x, y].west.activeSelf && x - 1 >= 0) yield return (x - 1, y);
    }

    (int fx, int fy) FarthestCellFrom(int sx, int sy)
    {
        var dist = new int[width, height];
        for (int i = 0; i < width; i++) for (int j = 0; j < height; j++) dist[i, j] = -1;

        Queue<(int x, int y)> q = new();
        q.Enqueue((sx, sy));
        dist[sx, sy] = 0;

        (int bx, int by) best = (sx, sy);

        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            foreach (var (nx, ny) in OpenNeighbors(x, y))
            {
                if (dist[nx, ny] != -1) continue;
                dist[nx, ny] = dist[x, y] + 1;
                q.Enqueue((nx, ny));
                if (dist[nx, ny] > dist[best.bx, best.by]) best = (nx, ny);
            }
        }
        return best;
    }

    // ---------- Generated outer walls ----------
    public Rect OuterRect()
    {
        var o = MazeOrigin();
        float W = width * cellSize;
        float H = height * cellSize;
        return new Rect(o.x, o.z, W, H); // x,z plane
    }

public void BuildOuterWalls()
{
    if (!outerWallPrefab)
    {
        Debug.LogError("[Maze] Assign outerWallPrefab (Cube with BoxCollider).");
        return;
    }

    if (wallW) DestroyImmediate(wallW);
    if (wallE) DestroyImmediate(wallE);
    if (wallS) DestroyImmediate(wallS);
    if (wallN) DestroyImmediate(wallN);

    var o = MazeOrigin();
    float W = width  * cellSize;
    float H = height * cellSize;

    float halfT      = wallThickness * 0.5f;      // outer-wall half thickness
    float cellHalfT  = cellWallThickness * 0.5f;  // cell-wall half thickness

    // Nudge the outer walls *inward* by the cell wall half-thickness so their
    // inner faces align with the centers of the cell walls.
    Vector3 westC  = new(o.x - halfT + cellHalfT,     wallHeight * 0.5f, o.z + H * 0.5f);
    Vector3 eastC  = new(o.x + W + halfT - cellHalfT, wallHeight * 0.5f, o.z + H * 0.5f);
    Vector3 southC = new(o.x + W * 0.5f,              wallHeight * 0.5f, o.z - halfT + cellHalfT);
    Vector3 northC = new(o.x + W * 0.5f,              wallHeight * 0.5f, o.z + H + halfT - cellHalfT);

    // Slightly extend lengths so corners meet neatly (prevents tiny gaps)
    float extend = cellWallThickness;
    wallW = MakeWall("WallW", westC,  new Vector3(wallThickness, wallHeight, H + extend),  WallDir.West,  WallEW);
    wallE = MakeWall("WallE", eastC,  new Vector3(wallThickness, wallHeight, H + extend),  WallDir.East,  WallEW);
    wallS = MakeWall("WallS", southC, new Vector3(W + extend,    wallHeight, wallThickness), WallDir.South, WallNS);
    wallN = MakeWall("WallN", northC, new Vector3(W + extend,    wallHeight, wallThickness), WallDir.North, WallNS);
}

    // public void BuildOuterWalls()
    // {
    //     if (!outerWallPrefab)
    //     {
    //         Debug.LogError("[Maze] Assign outerWallPrefab (Cube with BoxCollider).");
    //         return;
    //     }

    //     if (wallW) DestroyImmediate(wallW);
    //     if (wallE) DestroyImmediate(wallE);
    //     if (wallS) DestroyImmediate(wallS);
    //     if (wallN) DestroyImmediate(wallN);

    //     var o = MazeOrigin();
    //     float W = width * cellSize;
    //     float H = height * cellSize;
    //     float halfT = wallThickness * 0.5f;

    //     Vector3 westC = new(o.x - halfT, wallHeight * 0.5f, o.z + H * 0.5f);
    //     Vector3 eastC = new(o.x + W + halfT, wallHeight * 0.5f, o.z + H * 0.5f);
    //     Vector3 southC = new(o.x + W * 0.5f, wallHeight * 0.5f, o.z - halfT);
    //     Vector3 northC = new(o.x + W * 0.5f, wallHeight * 0.5f, o.z + H + halfT);

    //     wallW = MakeWall("WallW", westC, new Vector3(wallThickness, wallHeight, H), WallDir.West, WallEW);
    //     wallE = MakeWall("WallE", eastC, new Vector3(wallThickness, wallHeight, H), WallDir.East, WallEW);
    //     wallS = MakeWall("WallS", southC, new Vector3(W, wallHeight, wallThickness), WallDir.South, WallNS);
    //     wallN = MakeWall("WallN", northC, new Vector3(W, wallHeight, wallThickness), WallDir.North, WallNS);

    // }
    GameObject MakeWall(string name, Vector3 pos, Vector3 scale, WallDir dir, Material mat)
    {
        var go = Instantiate(outerWallPrefab, pos, Quaternion.identity, wallsParent ? wallsParent : transform);
        go.name = name;
        go.transform.localScale = scale;

        // Ensure collider exists
        var bc = go.GetComponent<BoxCollider>();
        if (!bc) bc = go.AddComponent<BoxCollider>();
        bc.size = Vector3.one;
        bc.center = Vector3.zero;
        go.isStatic = true;

        // Apply material + per-instance tiling
        var mr = go.GetComponent<MeshRenderer>();
        if (!mr) mr = go.AddComponent<MeshRenderer>();
        if (mat) mr.sharedMaterial = mat; // assign the per-wall material

        ApplyWallAppearance(mr, scale, dir);
        return go;
    }

    // GameObject MakeWall(string name, Vector3 pos, Vector3 scale)
    // {
    //     var go = Instantiate(outerWallPrefab, pos, Quaternion.identity, wallsParent ? wallsParent : transform);
    //     go.name = name;
    //     go.transform.localScale = scale;

    //     var bc = go.GetComponent<BoxCollider>();
    //     if (!bc) bc = go.AddComponent<BoxCollider>();
    //     bc.size = Vector3.one;
    //     bc.center = Vector3.zero;

    //     go.isStatic = true;
    //     return go;
    // }

    void ApplyWallAppearance(MeshRenderer mr, Vector3 scale, WallDir dir)
    {
        if (!mr || mr.sharedMaterial == null) return;

        // Which axis is the long dimension on this wall?
        float length = (dir == WallDir.West || dir == WallDir.East) ? scale.z : scale.x;
        float height = scale.y;

        // How many repeats based on physical size
        float tilesX = Mathf.Max(1f, length / Mathf.Max(0.0001f, tileMeters));
        float tilesY = Mathf.Max(1f, height / Mathf.Max(0.0001f, tileMeters));

        // Make sure the texture can repeat (safe at runtime)
        var tex = mr.sharedMaterial.mainTexture;
        if (tex) tex.wrapMode = TextureWrapMode.Repeat;

        // URP/HDRP Lit uses _BaseMap; Built-in Standard uses _MainTex
        // Use a MaterialPropertyBlock so we don't duplicate materials.
        var mpb = new MaterialPropertyBlock();

        // Try URP/HDRP first
        if (mr.sharedMaterial.HasProperty("_BaseMap"))
        {
            mr.GetPropertyBlock(mpb);
            // _BaseMap_ST = (scaleX, scaleY, offsetX, offsetY)
            mpb.SetVector("_BaseMap_ST", new Vector4(tilesX, tilesY, 0f, 0f));
            mr.SetPropertyBlock(mpb);
        }
        else
        {
            // Built-in Standard fallback
            // Note: accessing mr.material creates an instance; OK for a few walls.
            var inst = mr.material;
            if (inst.HasProperty("_MainTex"))
            {
                inst.mainTextureScale = new Vector2(tilesX, tilesY);
            }
        }
    }


    public Bounds GetInnerBounds()
    {
        var r = OuterRect();
        var center = new Vector3(r.center.x, 0, r.center.y);
        var size = new Vector3(r.width, 0, r.height);
        return new Bounds(center, size);
    }

    public Vector3 ClampInsideOuter(Vector3 pos, float margin = 0f)
    {
        var r = OuterRect();
        pos.x = Mathf.Clamp(pos.x, r.xMin + margin, r.xMax - margin);
        pos.z = Mathf.Clamp(pos.z, r.yMin + margin, r.yMax - margin);
        return pos;
    }

    // #if UNITY_EDITOR
    //     void OnDrawGizmosSelected()
    //     {
    //         Gizmos.color = Color.red;
    //         var r = OuterRect();
    //         Vector3 c = new(r.xMin + r.width * 0.5f, 0, r.yMin + r.height * 0.5f);
    //         Gizmos.DrawWireCube(c, new Vector3(r.width, 0.01f, r.height));
    //     }
    // #endif

#if UNITY_EDITOR
void OnDrawGizmosSelected()
{
    // OuterRect (what walls use)
    var r = OuterRect();
    Vector3 c = new(r.xMin + r.width * 0.5f, 0, r.yMin + r.height * 0.5f);
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(c, new Vector3(r.width, 0.01f, r.height));

    // First cell’s actual footprint (after scaling) for quick compare
    if (Application.isPlaying && grid != null)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(CellCenter(0,0), new Vector3(cellSize, 0.01f, cellSize));
    }
}
#endif

}
// using UnityEngine;
// using System.Collections.Generic;

// public class Cell
// {
//     public bool visited;
//     public GameObject north, south, east, west;
// }

// public class MazeGenerator : MonoBehaviour
// {
//     [Header("Size")]
//     public int width = 1, height = 1;

//     [Header("Refs")]
//     public GameObject cellPrefab;
//     public float cellSize = 3f;
//     public Transform player;
//     public Transform goal;

//     [Header("Options")]
//     public bool keepPerimeterClosed = true;

//     private Cell[,] grid;
//     private System.Random rng;

//     void Start()
//     {
//         rng = new System.Random();
//         BuildGrid();
//         CarveMazeDFS(0, 0);

//         if (keepPerimeterClosed)
//             EnsurePerimeterWallsActive();

//         if (player != null)
//             player.position = CellCenter(0, 0) + Vector3.up * 1f;

//         var (gx, gy) = FarthestCellFrom(0, 0);
//         if (goal != null)
//             goal.position = CellCenter(gx, gy);
//     }

//     public Vector3 CellCenter(int x, int y)
//         => new Vector3(x * cellSize, 0f, y * cellSize);

//     // --- grid build ---------------------------------------------------------

//     void BuildGrid()
//     {
//         grid = new Cell[width, height];
//         for (int x = 0; x < width; x++)
//         {
//             for (int y = 0; y < height; y++)
//             {
//                 var cellGO = Instantiate(
//                     cellPrefab,
//                     new Vector3(x * cellSize, 0, y * cellSize),
//                     Quaternion.identity,
//                     transform
//                 );

//                 // Use recursive find so nested walls still work
//                 var t = cellGO.transform;
//                 var north = FindDeepChild(t, "WallN");
//                 var south = FindDeepChild(t, "WallS");
//                 var east  = FindDeepChild(t, "WallE");
//                 var west  = FindDeepChild(t, "WallW");

//                 // Helpful logs if any are missing (name mismatch in prefab)
//                 if (north == null || south == null || east == null || west == null)
//                     Debug.LogWarning($"[MazeGenerator] Missing wall(s) on cell ({x},{y}). " +
//                                      $"Expected child names: WallN/WallS/WallE/WallW (can be nested).", cellGO);

//                 grid[x, y] = new Cell
//                 {
//                     visited = false,
//                     north = north != null ? north.gameObject : null,
//                     south = south != null ? south.gameObject : null,
//                     east  = east  != null ? east .gameObject : null,
//                     west  = west  != null ? west .gameObject : null
//                 };
//             }
//         }
//     }

//     // Recursive search for a child by name anywhere under 'parent'
//     Transform FindDeepChild(Transform parent, string name)
//     {
//         foreach (Transform child in parent)
//         {
//             if (child.name == name) return child;
//             var result = FindDeepChild(child, name);
//             if (result != null) return result;
//         }
//         return null;
//     }

//     // --- maze carve ---------------------------------------------------------

//     void CarveMazeDFS(int sx, int sy)
//     {
//         Stack<(int x, int y)> stack = new();
//         stack.Push((sx, sy));
//         grid[sx, sy].visited = true;

//         while (stack.Count > 0)
//         {
//             var (x, y) = stack.Peek();
//             var neighbors = GetUnvisitedNeighbors(x, y);

//             if (neighbors.Count == 0) { stack.Pop(); continue; }

//             var (nx, ny, dir) = neighbors[rng.Next(neighbors.Count)];
//             RemoveWallBetween(x, y, nx, ny, dir);
//             grid[nx, ny].visited = true;
//             stack.Push((nx, ny));
//         }
//     }

//     List<(int nx, int ny, string dir)> GetUnvisitedNeighbors(int x, int y)
//     {
//         var list = new List<(int, int, string)>();
//         if (y + 1 < height && !grid[x, y + 1].visited) list.Add((x, y + 1, "N"));
//         if (y - 1 >= 0     && !grid[x, y - 1].visited) list.Add((x, y - 1, "S"));
//         if (x + 1 < width  && !grid[x + 1, y].visited) list.Add((x + 1, y, "E"));
//         if (x - 1 >= 0     && !grid[x - 1, y].visited) list.Add((x - 1, y, "W"));
//         return list;
//     }

//     void RemoveWallBetween(int x, int y, int nx, int ny, string dir)
//     {
//         if (dir == "N") { SafeSetActive(grid[x, y].north, false); SafeSetActive(grid[nx, ny].south, false); }
//         if (dir == "S") { SafeSetActive(grid[x, y].south, false); SafeSetActive(grid[nx, ny].north, false); }
//         if (dir == "E") { SafeSetActive(grid[x, y].east , false); SafeSetActive(grid[nx, ny].west , false); }
//         if (dir == "W") { SafeSetActive(grid[x, y].west , false); SafeSetActive(grid[nx, ny].east , false); }
//     }

//     // --- perimeter enforcement ---------------------------------------------

//     void EnsurePerimeterWallsActive()
//     {
//         // West & East edges
//         for (int y = 0; y < height; y++)
//         {
//             ForceWallOn(grid[0, y]?.west);                 // West boundary
//             ForceWallOn(grid[width - 1, y]?.east);          // East boundary
//         }

//         // South & North edges
//         for (int x = 0; x < width; x++)
//         {
//             ForceWallOn(grid[x, 0]?.south);                 // South boundary
//             ForceWallOn(grid[x, height - 1]?.north);        // North boundary
//         }
//     }

//     void ForceWallOn(GameObject wall)
//     {
//         if (wall == null) return;
//         SafeSetActive(wall, true);

//         // Also re-enable renderer & collider in case they were disabled instead of the GO
//         var mr = wall.GetComponentInChildren<MeshRenderer>(true);
//         if (mr) mr.enabled = true;

//         var col = wall.GetComponentInChildren<Collider>(true);
//         if (col) col.enabled = true;
//     }

//     void SafeSetActive(GameObject go, bool state)
//     {
//         if (go != null && go.activeSelf != state) go.SetActive(state);
//     }

//     // --- pathfinding helpers ------------------------------------------------

//     public IEnumerable<(int nx, int ny)> OpenNeighbors(int x, int y)
//     {
//         if (grid[x, y].north != null && !grid[x, y].north.activeSelf && y + 1 < height) yield return (x, y + 1);
//         if (grid[x, y].south != null && !grid[x, y].south.activeSelf && y - 1 >= 0)     yield return (x, y - 1);
//         if (grid[x, y].east  != null && !grid[x, y].east .activeSelf && x + 1 < width)  yield return (x + 1, y);
//         if (grid[x, y].west  != null && !grid[x, y].west .activeSelf && x - 1 >= 0)     yield return (x - 1, y);
//     }

//     (int fx, int fy) FarthestCellFrom(int sx, int sy)
//     {
//         var dist = new int[width, height];
//         for (int i = 0; i < width; i++) for (int j = 0; j < height; j++) dist[i, j] = -1;

//         Queue<(int x, int y)> q = new();
//         q.Enqueue((sx, sy));
//         dist[sx, sy] = 0;

//         (int bx, int by) best = (sx, sy);

//         while (q.Count > 0)
//         {
//             var (x, y) = q.Dequeue();
//             foreach (var (nx, ny) in OpenNeighbors(x, y))
//             {
//                 if (dist[nx, ny] != -1) continue;
//                 dist[nx, ny] = dist[x, y] + 1;
//                 q.Enqueue((nx, ny));
//                 if (dist[nx, ny] > dist[best.bx, best.by]) best = (nx, ny);
//             }
//         }
//         return best;
//     }

// #if UNITY_EDITOR
//     // Visual sanity check for the extents
//     void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.red;
//         var size = new Vector3((width - 1) * cellSize, 0, (height - 1) * cellSize);
//         var center = new Vector3(size.x / 2f, 0, size.z / 2f);
//         Gizmos.DrawWireCube(center, new Vector3(size.x + cellSize, 0.01f, size.z + cellSize));
//     }
// #endif
// }
