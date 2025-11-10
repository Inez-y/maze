using UnityEngine;
using System.Collections.Generic;

#region Data
public class Cell
{
    public bool visited;
    public GameObject north, south, east, west;
}
#endregion

public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Size (inner playable area)")]
    public int width = 8, height = 8;    

    [Header("Cell Prefab & Layout")]
    public GameObject cellPrefab;
    public float cellSize = 2.0f;

    public bool cellsUseCenterPivot = true;
    public bool autoScaleCellToSize = true;

    [Header("Grid Expansion (Padding)")]
    public int padCellsX = 3;
    public int padCellsY = 3;
    public bool outerWalkwayOpen = true;
    public bool carveOnlyInnerRect = true;

    [Header("Perimeter Walls (from per-cell walls)")]
    public bool keepPerimeterClosed = true;

    [Header("Alignment")]
    public float cellWallThickness = 1f;

    [Header("Generated Outer Walls")]
    public GameObject outerWallPrefab;           // Cube with BoxCollider + MeshRenderer
    public float wallThickness = 0.25f;          // X for W/E, Z for S/N
    public float wallHeight = 2.0f;              // Y scale
    public float outerWallExpand = 1.5f;         // 1.0 = exact fit, 1.5 = 1.5x longer each side
    public Transform wallsParent;                // optional parent
    public bool buildOuterWalls = true;

    [Header("Outer Wall Materials & UV Tiling")]
    public Material WallEW;                      // material for West/East slabs
    public Material WallNS;                      // material for North/South slabs
    public float tileMeters = 1f;                // repeats per physical meter
    [Header("Scene Refs")]
    public Transform player;
 
    Cell[,] grid;
    System.Random rng;

    GameObject wallW, wallE, wallS, wallN;

    public bool IsReady { get; private set; }

    void Start()
    {
        IsReady = false;
        rng = new System.Random();

        BuildGrid();

        int sx = padCellsX, sy = padCellsY;
        if (carveOnlyInnerRect)
            CarveMazeDFSClamped(sx, sy, sx, sy, sx + width - 1, sy + height - 1);
        else
            CarveMazeDFS(sx, sy);

        if (outerWalkwayOpen)
            OpenPaddingRing(grid.GetLength(0), grid.GetLength(1));

        if (keepPerimeterClosed)
            EnsurePerimeterWallsActive(grid.GetLength(0), grid.GetLength(1));

        if (buildOuterWalls)
            BuildOuterWalls();                    // ← build ONCE

        if (player) player.position = CellCenter(sx, sy) + Vector3.up;

        IsReady = true;
    }


    // Bottom-left of whole grid (padding included) in world
    Vector3 MazeOrigin() => transform.position;

    public Vector3 CellCenter(int x, int y)
    {
        float h = cellSize * 0.5f;
        return MazeOrigin() + new Vector3(x * cellSize + h, 0f, y * cellSize + h);
    }

    void BuildGrid()
    {
        int totalW = width + padCellsX * 2;
        int totalH = height + padCellsY * 2;
        grid = new Cell[totalW, totalH];

        var origin = MazeOrigin();

        for (int x = 0; x < totalW; x++)
            for (int y = 0; y < totalH; y++)
            {
                Vector3 pos =
                    cellsUseCenterPivot
                        ? CellCenter(x, y)                                 // center placement
                        : origin + new Vector3(x * cellSize, 0, y * cellSize); // corner placement

                var cellGO = Instantiate(cellPrefab, pos, Quaternion.identity, transform);

                if (autoScaleCellToSize) FitCellFootprintToCellSize(cellGO);

                // Find named children (can be nested)
                var t = cellGO.transform;
                var north = FindDeepChild(t, "WallN");
                var south = FindDeepChild(t, "WallS");
                var east = FindDeepChild(t, "WallE");
                var west = FindDeepChild(t, "WallW");

                if (!north || !south || !east || !west)
                    Debug.LogWarning($"[Maze] Missing wall(s) on cell ({x},{y}). Expected WallN/WallS/WallE/WallW.", cellGO);

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
        var rends = cellGO.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        var parent = cellGO.transform.parent;
        Vector3 parentScale = parent ? parent.lossyScale : Vector3.one;

        float currentX = b.size.x / parentScale.x;
        float currentZ = b.size.z / parentScale.z;
        if (currentX <= 0.0001f || currentZ <= 0.0001f) return;

        float sx = cellSize / currentX;
        float sz = cellSize / currentZ;

        var local = cellGO.transform.localScale;
        cellGO.transform.localScale = new Vector3(local.x * sx, local.y, local.z * sz);
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var r = FindDeepChild(child, name);
            if (r != null) return r;
        }
        return null;
    }

    void CarveMazeDFS(int sx, int sy)
    {
        Stack<(int x, int y)> st = new();
        st.Push((sx, sy));
        grid[sx, sy].visited = true;

        while (st.Count > 0)
        {
            var (x, y) = st.Peek();
            var nbrs = GetUnvisitedNeighborsAny(x, y);
            if (nbrs.Count == 0) { st.Pop(); continue; }

            var (nx, ny, dir) = nbrs[rng.Next(nbrs.Count)];
            RemoveWallBetween(x, y, nx, ny, dir);
            grid[nx, ny].visited = true;
            st.Push((nx, ny));
        }
    }

    void CarveMazeDFSClamped(int sx, int sy, int minX, int minY, int maxX, int maxY)
    {
        Stack<(int x, int y)> st = new();
        st.Push((sx, sy));
        grid[sx, sy].visited = true;

        while (st.Count > 0)
        {
            var (x, y) = st.Peek();
            var nbrs = GetUnvisitedNeighborsClamped(x, y, minX, minY, maxX, maxY);
            if (nbrs.Count == 0) { st.Pop(); continue; }

            var (nx, ny, dir) = nbrs[rng.Next(nbrs.Count)];
            RemoveWallBetween(x, y, nx, ny, dir);
            grid[nx, ny].visited = true;
            st.Push((nx, ny));
        }
    }

    List<(int nx, int ny, string dir)> GetUnvisitedNeighborsAny(int x, int y)
    {
        int gw = grid.GetLength(0), gh = grid.GetLength(1);
        var L = new List<(int, int, string)>();
        if (y + 1 < gh && !grid[x, y + 1].visited) L.Add((x, y + 1, "N"));
        if (y - 1 >= 0 && !grid[x, y - 1].visited) L.Add((x, y - 1, "S"));
        if (x + 1 < gw && !grid[x + 1, y].visited) L.Add((x + 1, y, "E"));
        if (x - 1 >= 0 && !grid[x - 1, y].visited) L.Add((x - 1, y, "W"));
        return L;
    }

    List<(int nx, int ny, string dir)> GetUnvisitedNeighborsClamped(int x, int y, int minX, int minY, int maxX, int maxY)
    {
        var L = new List<(int, int, string)>();
        if (y + 1 <= maxY && !grid[x, y + 1].visited) L.Add((x, y + 1, "N"));
        if (y - 1 >= minY && !grid[x, y - 1].visited) L.Add((x, y - 1, "S"));
        if (x + 1 <= maxX && !grid[x + 1, y].visited) L.Add((x + 1, y, "E"));
        if (x - 1 >= minX && !grid[x - 1, y].visited) L.Add((x - 1, y, "W"));
        return L;
    }

    void RemoveWallBetween(int x, int y, int nx, int ny, string dir)
    {
        if (dir == "N") { SafeSetActive(grid[x, y].north, false); SafeSetActive(grid[nx, ny].south, false); }
        if (dir == "S") { SafeSetActive(grid[x, y].south, false); SafeSetActive(grid[nx, ny].north, false); }
        if (dir == "E") { SafeSetActive(grid[x, y].east, false); SafeSetActive(grid[nx, ny].west, false); }
        if (dir == "W") { SafeSetActive(grid[x, y].west, false); SafeSetActive(grid[nx, ny].east, false); }
    }

    // Open the padding ring so nothing is trapped
    void OpenPaddingRing(int totalW, int totalH)
    {
        int minX = padCellsX;
        int minY = padCellsY;
        int maxX = padCellsX + width - 1;
        int maxY = padCellsY + height - 1;

        for (int x = 0; x < totalW; x++)
            for (int y = 0; y < totalH; y++)
            {
                bool inInner = (x >= minX && x <= maxX && y >= minY && y <= maxY);
                if (inInner) continue;

                var c = grid[x, y];
                SafeSetActive(c.north, false);
                SafeSetActive(c.south, false);
                SafeSetActive(c.east, false);
                SafeSetActive(c.west, false);
            }

        // Make sure the inner border opens into the ring
        for (int x = minX; x <= maxX; x++)
        {
            SafeSetActive(grid[x, minY].south, false); // south edge of inner rect
            SafeSetActive(grid[x, maxY].north, false); // north edge
        }
        for (int y = minY; y <= maxY; y++)
        {
            SafeSetActive(grid[minX, y].west, false); // west edge
            SafeSetActive(grid[maxX, y].east, false); // east edge
        }
    }

    void EnsurePerimeterWallsActive(int totalW, int totalH)
    {
        // West/East edges
        for (int y = 0; y < totalH; y++)
        {
            ForceWallOn(grid[0, y]?.west);
            ForceWallOn(grid[totalW - 1, y]?.east);
        }
        // South/North edges
        for (int x = 0; x < totalW; x++)
        {
            ForceWallOn(grid[x, 0]?.south);
            ForceWallOn(grid[x, totalH - 1]?.north);
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

    public IEnumerable<(int nx, int ny)> OpenNeighbors(int x, int y)
    {
        int gw = grid.GetLength(0), gh = grid.GetLength(1);
        if (grid[x, y].north && !grid[x, y].north.activeSelf && y + 1 < gh) yield return (x, y + 1);
        if (grid[x, y].south && !grid[x, y].south.activeSelf && y - 1 >= 0) yield return (x, y - 1);
        if (grid[x, y].east && !grid[x, y].east.activeSelf && x + 1 < gw) yield return (x + 1, y);
        if (grid[x, y].west && !grid[x, y].west.activeSelf && x - 1 >= 0) yield return (x - 1, y);
    }


    enum WallDir { West, East, South, North }

    public Rect OuterRect()
    {
        var o = MazeOrigin();
        int totalW = width + padCellsX * 2;
        int totalH = height + padCellsY * 2;
        float W = totalW * cellSize;
        float H = totalH * cellSize;
        return new Rect(o.x, o.z, W, H); // x,z plane
    }

    public Bounds GetInnerBounds()
    {
        var r = OuterRect();
        var c = new Vector3(r.center.x, 0, r.center.y);
        var s = new Vector3(r.width, 0, r.height);
        return new Bounds(c, s);
    }

    public Vector3 ClampInsideOuter(Vector3 pos, float margin = 0f)
    {
        var r = OuterRect();
        pos.x = Mathf.Clamp(pos.x, r.xMin + margin, r.xMax - margin);
        pos.z = Mathf.Clamp(pos.z, r.yMin + margin, r.yMax - margin);
        return pos;
    }
public void BuildOuterWalls()
{
    if (!outerWallPrefab) { Debug.LogError("[Maze] Assign outerWallPrefab."); return; }

    if (wallW) DestroyImmediate(wallW);
    if (wallE) DestroyImmediate(wallE);
    if (wallS) DestroyImmediate(wallS);
    if (wallN) DestroyImmediate(wallN);

    // Tight rectangle around the padded grid
    Rect r = OuterRect(); // uses width + pad*2 (no +1)
    float W = r.width;
    float H = r.height;

    float halfT     = wallThickness * 0.5f;
    float cellHalfT = cellWallThickness * 0.5f;

    // Align inner faces to the cell-wall centers
    Vector3 westC  = new(r.xMin - halfT + cellHalfT, wallHeight * 0.5f, r.center.y);
    Vector3 eastC  = new(r.xMax + halfT - cellHalfT, wallHeight * 0.5f, r.center.y);
    Vector3 southC = new(r.center.x,                  wallHeight * 0.5f, r.yMin - halfT + cellHalfT);
    Vector3 northC = new(r.center.x,                  wallHeight * 0.5f, r.yMax + halfT - cellHalfT);

    // Slight extension to avoid tiny corner gaps
    float extend = cellWallThickness;

    wallW = MakeWall("outerWallW", westC,  new Vector3(wallThickness, wallHeight, H + extend), WallDir.West,  WallEW);
    wallE = MakeWall("outerWallE", eastC,  new Vector3(wallThickness, wallHeight, H + extend), WallDir.East,  WallEW);
    wallS = MakeWall("outerWallS", southC, new Vector3(W + extend,    wallHeight, wallThickness), WallDir.South, WallNS);
    wallN = MakeWall("outerWallN", northC, new Vector3(W + extend,    wallHeight, wallThickness), WallDir.North, WallNS);
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
    //     int totalW = width + padCellsX * 2 + 1;
    //     int totalH = height + padCellsY * 2 + 1;

    //     float W = totalW * cellSize;
    //     float H = totalH * cellSize;

    //     float halfT = wallThickness * 0.5f;
    //     float cellHalfT = cellWallThickness * 0.5f;

    //     // Expansion factor (1.0 = tight fit)
    //     float expand = Mathf.Max(1f, outerWallExpand);
    //     float extraX = (W * (expand - 1f)) * 0.5f;
    //     float extraZ = (H * (expand - 1f)) * 0.5f;

    //     // Align inner faces to cell-wall centers (nudge by cellHalfT)
    //     Vector3 westC = new(o.x - halfT - extraX + cellHalfT, wallHeight * 0.5f, o.z + H * 0.5f);
    //     Vector3 eastC = new(o.x + W + halfT + extraX - cellHalfT, wallHeight * 0.5f, o.z + H * 0.5f);
    //     Vector3 southC = new(o.x + W * 0.5f, wallHeight * 0.5f, o.z - halfT - extraZ + cellHalfT);
    //     Vector3 northC = new(o.x + W * 0.5f, wallHeight * 0.5f, o.z + H + halfT + extraZ - cellHalfT);

    //     // Slight extension so corners meet neatly
    //     float extend = cellWallThickness;

    //     wallW = MakeWall("outerWallW", westC, new Vector3(wallThickness, wallHeight, (H + extend) * expand), WallDir.West, WallEW);
    //     wallE = MakeWall("outerWallE", eastC, new Vector3(wallThickness, wallHeight, (H + extend) * expand), WallDir.East, WallEW);
    //     wallS = MakeWall("outerWallS", southC, new Vector3((W + extend) * expand, wallHeight, wallThickness), WallDir.South, WallNS);
    //     wallN = MakeWall("outerWallN", northC, new Vector3((W + extend) * expand, wallHeight, wallThickness), WallDir.North, WallNS);
    // }

    GameObject MakeWall(string name, Vector3 pos, Vector3 scale, WallDir dir, Material mat)
    {
        var go = Instantiate(outerWallPrefab, pos, Quaternion.identity, wallsParent ? wallsParent : transform);
        go.name = name;
        go.transform.localScale = scale;

        var bc = go.GetComponent<BoxCollider>();
        if (!bc) bc = go.AddComponent<BoxCollider>();
        bc.size = Vector3.one;
        bc.center = Vector3.zero;
        go.isStatic = true;

        var mr = go.GetComponent<MeshRenderer>();
        if (!mr) mr = go.AddComponent<MeshRenderer>();
        if (mat) mr.sharedMaterial = mat;

        ApplyWallAppearance(mr, scale, dir);
        return go;
    }

    void ApplyWallAppearance(MeshRenderer mr, Vector3 scale, WallDir dir)
    {
        if (!mr || mr.sharedMaterial == null) return;

        float length = (dir == WallDir.West || dir == WallDir.East) ? scale.z : scale.x;
        float height = scale.y;

        float tilesX = Mathf.Max(1f, length / Mathf.Max(0.0001f, tileMeters));
        float tilesY = Mathf.Max(1f, height / Mathf.Max(0.0001f, tileMeters));

        var tex = mr.sharedMaterial.mainTexture;
        if (tex) tex.wrapMode = TextureWrapMode.Repeat;

        var mpb = new MaterialPropertyBlock();
        if (mr.sharedMaterial.HasProperty("_BaseMap"))
        {
            mr.GetPropertyBlock(mpb);
            mpb.SetVector("_BaseMap_ST", new Vector4(tilesX, tilesY, 0f, 0f));
            mr.SetPropertyBlock(mpb);
        }
        else
        {
            var inst = mr.material;
            if (inst.HasProperty("_MainTex"))
                inst.mainTextureScale = new Vector2(tilesX, tilesY);
        }
    }

    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Outer rect (what outer slabs use)
        var r = OuterRect();
        Vector3 c = new(r.xMin + r.width * 0.5f, 0, r.yMin + r.height * 0.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(c, new Vector3(r.width, 0.01f, r.height));

        // Inner-rect corners (where carving occurs when clamped)
        int minX = padCellsX,                 minY = padCellsY;
        int maxX = padCellsX + width  - 1,    maxY = padCellsY + height - 1;
        Gizmos.color = new Color(0,1,0,0.4f);
        Vector3 a = CellCenter(minX, minY);
        Vector3 b = CellCenter(maxX, maxY);
        Vector3 innerSize = new(Mathf.Abs(b.x - a.x) + cellSize, 0.01f, Mathf.Abs(b.z - a.z) + cellSize);
        Vector3 innerCenter = (a + b) * 0.5f;
        Gizmos.DrawWireCube(innerCenter, innerSize);
    }
#endif
}
