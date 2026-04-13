using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerScoreDataManager : MonoBehaviour
{
    private PlayersScoreData _localPlayerScoreData;
    private JsonManager _jsonManager;
    private bool _isLoaded;

    private void Awake() { _jsonManager = new JsonManager(); }
    private void Start() { _jsonManager.TryReadFromJson(OnPlayerScoreDataLoaded, "PlayersScoreData.json"); }

    public void SavePlayersScoreData() { _jsonManager.TryWriteToJson(_localPlayerScoreData, "PlayersScoreData.json"); }
    public PlayersScoreData GetLocalPlayerScoreData()
    {
        if (_isLoaded) return _localPlayerScoreData;
        else
        {
            Debug.LogWarning("Player data wasn't loaded. Returning default");
            return new PlayersScoreData { PlayerData = new List<PlayerData>() };
        }
    }

    private void OnPlayerScoreDataLoaded(string data)
    {
        _localPlayerScoreData = JsonUtility.FromJson<PlayersScoreData>(data);
        Debug.Log($"EXAMPLE Player data loaded: {_localPlayerScoreData.PlayerData.Count} players\n{_localPlayerScoreData.PlayerData[0].PlayerName}, {_localPlayerScoreData.PlayerData[0].PlayerScore}, {_localPlayerScoreData.PlayerData[0].PlayerIndex}");
        _isLoaded = true;
    }

}

[Serializable]
public struct PlayersScoreData
{
    public List<PlayerData> PlayerData;
}

[Serializable]
public struct PlayerData
{
    public string PlayerName;
    public int PlayerScore;
    public int PlayerIndex;
}