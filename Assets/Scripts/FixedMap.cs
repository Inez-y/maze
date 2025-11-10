
using UnityEngine;
using System.Collections.Generic;

public class FixedMap : MonoBehaviour
{
    [Header("Grid")]
    public int width = 8;
    public int height = 8;
    public Transform cellsRoot;        
    public float yOffset = 0f;

    // storage
    Transform[,] cells;
    bool[,,] walls; 
    public bool IsReady { get; private set; }

    public struct Neighbor { public int nx, ny; public Neighbor(int x,int y){ nx=x; ny=y; } }

    void Awake()
    {
        BuildFromScene();
    }

    void BuildFromScene()
    {
        if (!cellsRoot)
        {
            Debug.LogError("[FixedMap] cellsRoot not assigned.");
            return;
        }

        cells = new Transform[width, height];
        walls = new bool[width, height, 4];

        // find cells by name: Cell_x_y
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var name = $"Cell_{x}_{y}";
            var t = cellsRoot.Find(name);
            if (!t)
            {
                Debug.LogWarning($"[FixedMap] Missing child: {name}. Using nearest match if any.");
                // optional: try slower search
                foreach (Transform c in cellsRoot)
                    if (c.name == name) { t = c; break; }
            }
            cells[x, y] = t;

            // read walls: if child exists && activeInHierarchy => blocked
            walls[x, y, 0] = HasActiveChild(t, "WallN"); // N
            walls[x, y, 1] = HasActiveChild(t, "WallE"); // E
            walls[x, y, 2] = HasActiveChild(t, "WallS"); // S
            walls[x, y, 3] = HasActiveChild(t, "WallW"); // W
        }

        IsReady = true;
    }

    bool HasActiveChild(Transform parent, string childName)
    {
        if (!parent) return false; // treat as open if missing parent (fail soft)
        var c = parent.Find(childName);
        return c && c.gameObject.activeInHierarchy;
    }

    public Vector3 CellCenter(int x, int y)
    {
        var t = cells[x, y];
        if (!t)
        {
            // fallback to grid spacing if the cell object is missing
            return new Vector3(x, 0f, y) + Vector3.up * yOffset;
        }
        var p = t.position;
        p.y = yOffset + p.y; // add offset on top of whatever the cell has
        return p;
    }

    public IEnumerable<Neighbor> OpenNeighbors(int x, int y)
    {
        // north
        if (!walls[x, y, 0] && y + 1 < height && !walls[x, y + 1, 2])
            yield return new Neighbor(x, y + 1);

        // east
        if (!walls[x, y, 1] && x + 1 < width && !walls[x + 1, y, 3])
            yield return new Neighbor(x + 1, y);

        // south
        if (!walls[x, y, 2] && y - 1 >= 0 && !walls[x, y - 1, 0])
            yield return new Neighbor(x, y - 1);

        // west
        if (!walls[x, y, 3] && x - 1 >= 0 && !walls[x - 1, y, 1])
            yield return new Neighbor(x - 1, y);
    }

    public Bounds GetInnerBounds()
    {
        // auto-build from cell positions (tight bounds)
        bool any = false;
        var b = new Bounds();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var t = cells[x, y];
            if (!t) continue;
            if (!any) { b = new Bounds(t.position, Vector3.one * 0.1f); any = true; }
            else b.Encapsulate(t.position);
        }
        if (!any) b = new Bounds(Vector3.zero, new Vector3(width, 0.1f, height));
        return b;
    }
}
