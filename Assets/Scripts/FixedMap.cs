using UnityEngine;

public class FixedMap : MonoBehaviour
{
    [Header("Grid Size")]
    [Min(1)] public int width  = 8;
    [Min(1)] public int height = 8;

    [Header("Hierarchy (optional)")]
    [Tooltip("If null, a 'CellsRoot' will be created automatically under this object at runtime.")]
    public Transform cellsRoot;

    [Header("Runtime Auto-Build")]
    [Tooltip("If true, the grid hierarchy (Cell_x_y and optional WallN/E/S/W children) will be created at runtime if missing.")]
    public bool autoBuildAtRuntime = true;

    // --- Public helpers your other scripts can use ---
    public Transform GetCell(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        var root = GetOrCreateRoot();
        var t = root.Find(CellName(x, y));
        if (!t && autoBuildAtRuntime)
        {
            t = CreateCell(root, x, y);
        }
        return t;
    }

    public bool TryWorldToCell(Vector3 world, out int x, out int y)
    {
        // Assuming each cell is 1x1 on XZ with origin at (0,0) of map transform.
        var local = transform.InverseTransformPoint(world);
        x = Mathf.RoundToInt(local.x);
        y = Mathf.RoundToInt(local.z);
        return IsInBounds(x, y);
    }

    public Vector3 CellCenterWorld(int x, int y)
    {
        var local = new Vector3(x, 0f, y);
        return transform.TransformPoint(local);
    }

    public void EnsureGridBuilt()
    {
        if (!autoBuildAtRuntime) return;
        var root = GetOrCreateRoot();
        // quickly check if at least one expected child exists
        if (root.Find(CellName(0, 0)) != null) return;

        for (int yy = 0; yy < height; yy++)
        for (int xx = 0; xx < width;  xx++)
            CreateCell(root, xx, yy);
    }

    // --- Internals ---
    void Awake()
    {
        if (autoBuildAtRuntime) EnsureGridBuilt();
    }

    bool IsInBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    string CellName(int x, int y) => $"Cell_{x}_{y}";

    Transform GetOrCreateRoot()
    {
        if (cellsRoot) return cellsRoot;
        var found = transform.Find("CellsRoot");
        if (found) return cellsRoot = found;

        var go = new GameObject("CellsRoot");
        go.transform.SetParent(transform, false);
        return cellsRoot = go.transform;
    }

    Transform CreateCell(Transform root, int x, int y)
    {
        var cell = new GameObject(CellName(x, y)).transform;
        cell.SetParent(root, false);
        cell.localPosition = new Vector3(x, 0f, y);

        // Optional wall markers (inactive by default; your logic can check activeSelf to mean “wall exists”)
        CreateChild(cell, "WallN", false);
        CreateChild(cell, "WallE", false);
        CreateChild(cell, "WallS", false);
        CreateChild(cell, "WallW", false);

        return cell;
    }

    static void CreateChild(Transform parent, string name, bool active)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.SetActive(active);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        width  = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
    }
#endif
}
