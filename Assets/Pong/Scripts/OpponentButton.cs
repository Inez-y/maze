using UnityEngine;

public class OpponentButton : MonoBehaviour
{
    public void StartAsPlayer()
    {
        EventManager.TriggerGameStart(PaddleType.Player);
    }

    public void StartAsAI()
    {
        EventManager.TriggerGameStart(PaddleType.AI);
    }
}
