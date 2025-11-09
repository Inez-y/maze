using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private GameObject winUI; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("YOU WIN!");

            if (winUI != null)
                winUI.SetActive(true);
            else
                Debug.LogWarning("⚠️ No Win UI assigned to GoalTrigger.");
        }
    }
}
