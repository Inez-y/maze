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

    void Start()
    {
        rng = new System.Random();
        BuildGrid();
        CarveMazeDFS(0, 0);

        if (keepPerimeterClosed)
            EnsurePerimeterWallsActive();

        if (player != null)
            player.position = CellCenter(0, 0) + Vector3.up * 1f;

        var (gx, gy) = FarthestCellFrom(0, 0);
        if (goal != null)
            goal.position = CellCenter(gx, gy);
    }

    public Vector3 CellCenter(int x, int y)
        => new Vector3(x * cellSize, 0f, y * cellSize);

    // --- grid build ---------------------------------------------------------

    void BuildGrid()
    {
        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var cellGO = Instantiate(
                    cellPrefab,
                    new Vector3(x * cellSize, 0, y * cellSize),
                    Quaternion.identity,
                    transform
                );

                // Use recursive find so nested walls still work
                var t = cellGO.transform;
                var north = FindDeepChild(t, "WallN");
                var south = FindDeepChild(t, "WallS");
                var east  = FindDeepChild(t, "WallE");
                var west  = FindDeepChild(t, "WallW");

                // Helpful logs if any are missing (name mismatch in prefab)
                if (north == null || south == null || east == null || west == null)
                    Debug.LogWarning($"[MazeGenerator] Missing wall(s) on cell ({x},{y}). " +
                                     $"Expected child names: WallN/WallS/WallE/WallW (can be nested).", cellGO);

                grid[x, y] = new Cell
                {
                    visited = false,
                    north = north != null ? north.gameObject : null,
                    south = south != null ? south.gameObject : null,
                    east  = east  != null ? east .gameObject : null,
                    west  = west  != null ? west .gameObject : null
                };
            }
        }
    }

    // Recursive search for a child by name anywhere under 'parent'
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

    // --- maze carve ---------------------------------------------------------

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
        if (y - 1 >= 0     && !grid[x, y - 1].visited) list.Add((x, y - 1, "S"));
        if (x + 1 < width  && !grid[x + 1, y].visited) list.Add((x + 1, y, "E"));
        if (x - 1 >= 0     && !grid[x - 1, y].visited) list.Add((x - 1, y, "W"));
        return list;
    }

    void RemoveWallBetween(int x, int y, int nx, int ny, string dir)
    {
        if (dir == "N") { SafeSetActive(grid[x, y].north, false); SafeSetActive(grid[nx, ny].south, false); }
        if (dir == "S") { SafeSetActive(grid[x, y].south, false); SafeSetActive(grid[nx, ny].north, false); }
        if (dir == "E") { SafeSetActive(grid[x, y].east , false); SafeSetActive(grid[nx, ny].west , false); }
        if (dir == "W") { SafeSetActive(grid[x, y].west , false); SafeSetActive(grid[nx, ny].east , false); }
    }

    // --- perimeter enforcement ---------------------------------------------

    void EnsurePerimeterWallsActive()
    {
        // West & East edges
        for (int y = 0; y < height; y++)
        {
            ForceWallOn(grid[0, y]?.west);                 // West boundary
            ForceWallOn(grid[width - 1, y]?.east);          // East boundary
        }

        // South & North edges
        for (int x = 0; x < width; x++)
        {
            ForceWallOn(grid[x, 0]?.south);                 // South boundary
            ForceWallOn(grid[x, height - 1]?.north);        // North boundary
        }
    }

    void ForceWallOn(GameObject wall)
    {
        if (wall == null) return;
        SafeSetActive(wall, true);

        // Also re-enable renderer & collider in case they were disabled instead of the GO
        var mr = wall.GetComponentInChildren<MeshRenderer>(true);
        if (mr) mr.enabled = true;

        var col = wall.GetComponentInChildren<Collider>(true);
        if (col) col.enabled = true;
    }

    void SafeSetActive(GameObject go, bool state)
    {
        if (go != null && go.activeSelf != state) go.SetActive(state);
    }

    // --- pathfinding helpers ------------------------------------------------

    public IEnumerable<(int nx, int ny)> OpenNeighbors(int x, int y)
    {
        if (grid[x, y].north != null && !grid[x, y].north.activeSelf && y + 1 < height) yield return (x, y + 1);
        if (grid[x, y].south != null && !grid[x, y].south.activeSelf && y - 1 >= 0)     yield return (x, y - 1);
        if (grid[x, y].east  != null && !grid[x, y].east .activeSelf && x + 1 < width)  yield return (x + 1, y);
        if (grid[x, y].west  != null && !grid[x, y].west .activeSelf && x - 1 >= 0)     yield return (x - 1, y);
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

#if UNITY_EDITOR
    // Visual sanity check for the extents
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var size = new Vector3((width - 1) * cellSize, 0, (height - 1) * cellSize);
        var center = new Vector3(size.x / 2f, 0, size.z / 2f);
        Gizmos.DrawWireCube(center, new Vector3(size.x + cellSize, 0.01f, size.z + cellSize));
    }
#endif
}
