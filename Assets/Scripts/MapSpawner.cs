using UnityEngine;

public class MapSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject fixedMapPrefab;

    [Header("Spawn")]
    public Vector3 spawnPosition = Vector3.zero;
    public Quaternion spawnRotation = Quaternion.identity;

    public static FixedMap ActiveMap { get; private set; }

    void Awake()
    {
        if (!fixedMapPrefab)
        {
            Debug.LogError("[MapSpawner] No FixedMap prefab assigned.");
            return;
        }

        var go = Instantiate(fixedMapPrefab, spawnPosition, spawnRotation);
        ActiveMap = go.GetComponent<FixedMap>();
        if (!ActiveMap)
        {
            Debug.LogError("[MapSpawner] Spawned prefab has no FixedMap component.");
            return;
        }

        // Optional: auto-wire any enemies already in the scene
        var enemies = FindObjectsOfType<EnemyControllerFSM>();
        foreach (var e in enemies)
            if (!e.maze) e.maze = ActiveMap;
    }
}
