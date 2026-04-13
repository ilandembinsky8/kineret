using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerNameGenerator
{
    public static string GetName()
    {
        int index = PlayerScoreDataManager.Instance.GetLocalPlayerScoreData().GamesPlayedIndex;
        return $"KIC{index:D4}";
    }
}
