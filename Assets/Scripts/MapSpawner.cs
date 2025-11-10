using UnityEngine;

[DefaultExecutionOrder(-500)]
public class MapSpawner : MonoBehaviour
{
    [Header("Prefab (must contain a FixedMap somewhere in its hierarchy)")]
    public GameObject fixedMapPrefab;

    [Header("Spawn")]
    public Vector3    spawnPosition = Vector3.zero;
    public Quaternion spawnRotation = Quaternion.identity;

    [Header("Convenience")]
    [Tooltip("If true, automatically assigns the spawned FixedMap to any EnemyControllerFSM in the scene that has maze == null.")]
    public bool autoWireEnemies = true;

    public static FixedMap ActiveMap { get; private set; }

    void Awake()
    {
        if (!fixedMapPrefab)
        {
            Debug.LogError("[MapSpawner] No FixedMap prefab assigned on MapSpawner.", this);
            return;
        }

        var go = Instantiate(fixedMapPrefab, spawnPosition, spawnRotation);
        ActiveMap = go.GetComponentInChildren<FixedMap>(true);
        if (!ActiveMap)
        {
            Debug.LogError($"[MapSpawner] Prefab '{fixedMapPrefab.name}' has no FixedMap component anywhere in its hierarchy.", go);
            return;
        }

        // ensure the map is usable even if the hierarchy is empty
        ActiveMap.EnsureGridBuilt();

        if (autoWireEnemies)
        {
            var enemies = FindAllByType<EnemyControllerFSM>();
            foreach (var e in enemies)
                if (e && !e.maze) e.maze = ActiveMap;
        }
    }

    void OnDestroy()
    {
        if (ActiveMap && ActiveMap.gameObject && ActiveMap.gameObject.scene != gameObject.scene)
            return; // different scene kept it
        ActiveMap = null;
    }

    // ---------- helpers to use new APIs on 2023+ while staying compatible ----------
    static T[] FindAllByType<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
#pragma warning disable CS0618
        return Object.FindObjectsOfType<T>();
#pragma warning restore CS0618
#endif
    }
}
