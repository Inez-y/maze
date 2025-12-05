using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class PersistenceManager : MonoBehaviour
{
    public GameObject player;
    public GameObject enemy;
    public ScoreManager sm;

    private string savePath;

    [System.Serializable]
    private class SaveData
    {
        public Vector3 playerPos;
        public Vector3 enemyPos;
        public int score;
    }

    private void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "save.json");
    }

    private void Start()
    {
        LoadGame();
    }

    public void OnSave(InputValue value)
    {
        if (!value.isPressed) return;
        SaveGame();
    }

    private void SaveGame()
    {
        if (player == null || enemy == null || sm == null)
        {
            Debug.LogWarning("PersistenceManager: Missing references.");
            return;
        }

        SaveData data = new SaveData
        {
            playerPos = player.transform.position,
            enemyPos  = enemy.transform.position,
            score     = sm.Score
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"Game saved to: {savePath}");
    }

    private void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("No save file found — starting fresh.");
            return;
        }

        string json = File.ReadAllText(savePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (player != null)
            player.transform.position = data.playerPos;

        if (enemy != null)
            enemy.transform.position = data.enemyPos;

        if (sm != null)
            sm.Score = data.score;

        Debug.Log("Game loaded from save file.");
    }
}
