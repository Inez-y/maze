using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMProximityModulator : MonoBehaviour
{
    [Header("Scene Refs")]
    public Transform player;
    public Transform enemy;

    [Header("Distance → Volume")]
    public float minDistance = 1f;    
    public float maxDistance = 20f; 
    [Range(0f, 1f)] public float minVolume = 0.1f;
    [Range(0f, 1f)] public float maxVolume = 0.8f;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!player || !enemy) return;
        if (!source.isPlaying) return;   

        float dist = Vector3.Distance(player.position, enemy.position);

        // t = 0 when far, 1 when close
        float t = Mathf.InverseLerp(maxDistance, minDistance, dist);

        source.volume = Mathf.Lerp(minVolume, maxVolume, t);
    }
}

