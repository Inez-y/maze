
// using UnityEngine;
// using System.Collections.Generic;

// public class FixedMap : MonoBehaviour
// {
//     [Header("Grid")]
//     public int width = 8;
//     public int height = 8;
//     // public Transform cellsRoot;        
//     public float yOffset = 0f;

//     // storage
//     Transform[,] cells;
//     bool[,,] walls; 
//     public bool IsReady { get; private set; }

//     public struct Neighbor { public int nx, ny; public Neighbor(int x,int y){ nx=x; ny=y; } }
//     [Header("Prefabs")]
//     public GameObject Map;   

//     [Header("Spawn Settings")]
//     public Vector3 spawnPosition = Vector3.zero;
//     public Quaternion spawnRotation = Quaternion.identity;

//     void Start()
//     {
//         SpawnMaze();
//     }

//     void SpawnMaze()
//     {
//         if (!Map)
//         {
//             Debug.LogError("No FixedMap prefab assigned!");
//             return;
//         }

//         // Instantiate the prefab in the scene
//         GameObject mazeInstance = Instantiate(Map, spawnPosition, spawnRotation);

//         // Optional: get the FixedMap script from it
//         FixedMap map = mazeInstance.GetComponent<FixedMap>();
//         if (map != null)
//         {
//             Debug.Log("Maze spawned successfully!");
//             // You can even trigger rebuild or path access here
//             // e.g. map.BuildFromScene();
//         }
//     }


//     bool HasActiveChild(Transform parent, string childName)
//     {
//         if (!parent) return false; // treat as open if missing parent (fail soft)
//         var c = parent.Find(childName);
//         return c && c.gameObject.activeInHierarchy;
//     }

//     public Vector3 CellCenter(int x, int y)
//     {
//         var t = cells[x, y];
//         if (!t)
//         {
//             // fallback to grid spacing if the cell object is missing
//             return new Vector3(x, 0f, y) + Vector3.up * yOffset;
//         }
//         var p = t.position;
//         p.y = yOffset + p.y; // add offset on top of whatever the cell has
//         return p;
//     }

//     public IEnumerable<Neighbor> OpenNeighbors(int x, int y)
//     {
//         // north
//         if (!walls[x, y, 0] && y + 1 < height && !walls[x, y + 1, 2])
//             yield return new Neighbor(x, y + 1);

//         // east
//         if (!walls[x, y, 1] && x + 1 < width && !walls[x + 1, y, 3])
//             yield return new Neighbor(x + 1, y);

//         // south
//         if (!walls[x, y, 2] && y - 1 >= 0 && !walls[x, y - 1, 0])
//             yield return new Neighbor(x, y - 1);

//         // west
//         if (!walls[x, y, 3] && x - 1 >= 0 && !walls[x - 1, y, 1])
//             yield return new Neighbor(x - 1, y);
//     }

//     public Bounds GetInnerBounds()
//     {
//         // auto-build from cell positions (tight bounds)
//         bool any = false;
//         var b = new Bounds();
//         for (int y = 0; y < height; y++)
//         for (int x = 0; x < width; x++)
//         {
//             var t = cells[x, y];
//             if (!t) continue;
//             if (!any) { b = new Bounds(t.position, Vector3.one * 0.1f); any = true; }
//             else b.Encapsulate(t.position);
//         }
//         if (!any) b = new Bounds(Vector3.zero, new Vector3(width, 0.1f, height));
//         return b;
//     }
// }
using UnityEngine;
using System.Collections.Generic;

public class FixedMap : MonoBehaviour
{
    [Header("Grid")]
    public int width  = 8;
    public int height = 8;
    [Tooltip("Parent that contains Cell_x_y children. If left empty, will try to find a child named 'CellsRoot', else use this transform.")]
    public Transform cellsRoot;
    public float yOffset = 0f;

    // storage
    Transform[,] cells;
    bool[,,] walls; // [x,y,dir] true=blocked; N=0,E=1,S=2,W=3
    public bool IsReady { get; private set; }

    public struct Neighbor { public int nx, ny; public Neighbor(int x,int y){ nx=x; ny=y; } }

    void Awake()
    {
        // resolve cellsRoot
        if (!cellsRoot)
        {
            var found = transform.Find("CellsRoot");
            cellsRoot = found ? found : transform;
        }

        BuildFromScene();
        IsReady = true;
    }

    void BuildFromScene()
    {
        if (!cellsRoot)
        {
            Debug.LogError("[FixedMap] cellsRoot not assigned/found.");
            return;
        }

        cells = new Transform[width, height];
        walls = new bool[width, height, 4];

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width;  x++)
        {
            var t = FindCell(x, y);
            cells[x, y] = t;

            // read walls: active child => blocked
            walls[x, y, 0] = HasActiveChild(t, "WallN"); // N
            walls[x, y, 1] = HasActiveChild(t, "WallE"); // E
            walls[x, y, 2] = HasActiveChild(t, "WallS"); // S
            walls[x, y, 3] = HasActiveChild(t, "WallW"); // W
        }
    }

    Transform FindCell(int x, int y)
    {
        if (!cellsRoot) return null;
        var name = $"Cell_{x}_{y}";
        var t = cellsRoot.Find(name);
        if (!t)
        {
            // slow path (safe): linear search
            foreach (Transform c in cellsRoot)
                if (c.name == name) { t = c; break; }
            if (!t)
                Debug.LogWarning($"[FixedMap] Missing child: {name}.");
        }
        return t;
    }

    bool HasActiveChild(Transform parent, string childName)
    {
        if (!parent) return false; // treat as open
        var c = parent.Find(childName);
        return c && c.gameObject.activeInHierarchy;
    }

    public Vector3 CellCenter(int x, int y)
    {
        var t = cells != null ? cells[x, y] : null;
        if (!t)
            return new Vector3(x, 0f, y) + Vector3.up * yOffset;
        var p = t.position;
        p.y = p.y + yOffset;
        return p;
    }

    public IEnumerable<Neighbor> OpenNeighbors(int x, int y)
    {
        // guard against uninitialized arrays
        if (walls == null) yield break;

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
        bool any = false;
        var b = new Bounds();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width;  x++)
        {
            var t = (cells != null) ? cells[x, y] : FindCell(x, y);
            if (!t) continue;
            if (!any) { b = new Bounds(t.position, Vector3.one * 0.1f); any = true; }
            else b.Encapsulate(t.position);
        }
        if (!any) b = new Bounds(Vector3.zero, new Vector3(width, 0.1f, height));
        return b;
    }
}
