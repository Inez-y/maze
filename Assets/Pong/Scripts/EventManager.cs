using System;
using UnityEngine;

public static class EventManager
{
    public static event Action<PaddleType> GameStart;
    public static event Action<int> PointScored;
    public static event Action<int> GameEnd;

    public static void TriggerGameStart(PaddleType pt)
    {
        GameStart?.Invoke(pt);
    }

    public static void TriggerPointScored(int winner) 
    {
        PointScored?.Invoke(winner);
    }

    public static void TriggerGameEnd(int winner) 
    {
        GameEnd?.Invoke(winner);
    }
}