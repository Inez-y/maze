using UnityEngine;
using System.Collections.Generic;

// Cell reference
public class Cell
{
    public bool visited;
    public GameObject north, south, east, west;
}

public class MazeGenerator : MonoBehaviour {
    [Header("Size")]
    public int width = 1, height = 1;

    [Header("Refs")]
    public GameObject cellPrefab;
    public float cellSize = 3f;
     public Transform player;
    public Transform goal;   

    private Cell[,] grid;
    private System.Random rng;

    void Start()
    {
        rng = new System.Random();
        BuildGrid();
        CarveMazeDFS(0, 0);
        // PlacePlayerAndGoal();

        // Place player at start
        if (player != null)
            player.position = CellCenter(0, 0) + Vector3.up * 1f;

        // Find farthest cell from start and place goal there
        var (gx, gy) = FarthestCellFrom(0, 0);
        if (goal != null)
            goal.position = CellCenter(gx, gy);
    }
    
    public Vector3 CellCenter(int x, int y)
        => new Vector3(x * cellSize, 0f, y * cellSize);

    void BuildGrid() {
        grid = new Cell[width, height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                var cellGO = Instantiate(cellPrefab,
                    new Vector3(x * cellSize, 0, y * cellSize),
                    Quaternion.identity, transform);

                var walls = cellGO.transform;
                grid[x, y] = new Cell {
                    visited = false,
                    north = walls.Find("WallN").gameObject,
                    south = walls.Find("WallS").gameObject,
                    east  = walls.Find("WallE").gameObject,
                    west  = walls.Find("WallW").gameObject
                };
            }
        }
    }

    void CarveMazeDFS(int sx, int sy) {
        Stack<(int x, int y)> stack = new();
        stack.Push((sx, sy));
        grid[sx, sy].visited = true;

        while (stack.Count > 0) {
            var (x, y) = stack.Peek();
            var neighbors = GetUnvisitedNeighbors(x, y);

            if (neighbors.Count == 0) { stack.Pop(); continue; }

            var (nx, ny, dir) = neighbors[rng.Next(neighbors.Count)];
            RemoveWallBetween(x, y, nx, ny, dir);
            grid[nx, ny].visited = true;
            stack.Push((nx, ny));
        }
    }

    List<(int nx, int ny, string dir)> GetUnvisitedNeighbors(int x, int y) {
        var list = new List<(int,int,string)>();
        if (y+1 < height && !grid[x,y+1].visited) list.Add((x, y+1, "N"));
        if (y-1 >= 0     && !grid[x,y-1].visited) list.Add((x, y-1, "S"));
        if (x+1 < width  && !grid[x+1,y].visited) list.Add((x+1, y, "E"));
        if (x-1 >= 0     && !grid[x-1,y].visited) list.Add((x-1, y, "W"));
        return list;
    }

    void RemoveWallBetween(int x, int y, int nx, int ny, string dir)
    {
        // Remove at current & neighbor
        if (dir == "N") { grid[x, y].north.SetActive(false); grid[nx, ny].south.SetActive(false); }
        if (dir == "S") { grid[x, y].south.SetActive(false); grid[nx, ny].north.SetActive(false); }
        if (dir == "E") { grid[x, y].east.SetActive(false); grid[nx, ny].west.SetActive(false); }
        if (dir == "W") { grid[x, y].west.SetActive(false); grid[nx, ny].east.SetActive(false); }
    }

    // void PlacePlayerAndGoal() {
    //     // Spawn points at opposite corners (customize as needed)
    //     var player = GameObject.FindWithTag("Player");
    //     if (player != null) player.transform.position = new Vector3(0, 1, 0);

    //     var goal = GameObject.FindWithTag("Goal");
    //     if (goal != null) goal.transform.position = new Vector3((width-1)*cellSize, 0, (height-1)*cellSize);
    // }
    
    // Farthest cell
    public IEnumerable<(int nx,int ny)> OpenNeighbors(int x, int y)
    {
        if (!grid[x,y].north.activeSelf && y+1 < height) yield return (x, y+1);
        if (!grid[x,y].south.activeSelf && y-1 >= 0)     yield return (x, y-1);
        if (!grid[x,y].east .activeSelf && x+1 < width)  yield return (x+1, y);
        if (!grid[x,y].west .activeSelf && x-1 >= 0)     yield return (x-1, y);
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
    
    // Enemy logic
    // public Vector3 CellCenter(int x, int y)
    // => new Vector3(x * cellSize, 0f, y * cellSize);

    // public IEnumerable<(int nx,int ny)> OpenNeighbors(int x, int y)
    // {
    //     // A neighbor is open if the shared wall GameObject is INACTIVE
    //     if (!grid[x,y].north.activeSelf && y+1 < height) yield return (x, y+1);
    //     if (!grid[x,y].south.activeSelf && y-1 >= 0)     yield return (x, y-1);
    //     if (!grid[x,y].east .activeSelf && x+1 < width)  yield return (x+1, y);
    //     if (!grid[x,y].west .activeSelf && x-1 >= 0)     yield return (x-1, y);
    // }

    // convert world position to nearest cell indices?
    // public (int cx,int cy) WorldToCell(Vector3 pos)
    // {
    //     int cx = Mathf.Clamp(Mathf.RoundToInt(pos.x / cellSize), 0, width-1);
    //     int cy = Mathf.Clamp(Mathf.RoundToInt(pos.z / cellSize), 0, height-1);
    //     return (cx, cy);
    // }


}
