
using UnityEngine;
using System;
using System.Collections.Generic;

public class HighscoresManager : MonoBehaviour
{
    public static HighscoresManager Instance { get; private set; }

    public Highscores Highscores { get; private set; }

    public int CurrentUserID;

    private JsonManager _jsonManager;

    private int _maxUsers;
    private int _maxID;
    private float _removeUsersPercentage;
    private string _userHeader;
    private List<int> _availableIDs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _jsonManager = new JsonManager();

        _maxID = GameSettingsManager.GetInt("Highscores Settings", "MaxIDExculsive", 10000);
        _maxUsers = GameSettingsManager.GetInt("Highscores Settings", "MaxUsers", 100);
        _userHeader = GameSettingsManager.Get("Highscores Settings", "UserHeader", "KIC");
        _removeUsersPercentage = GameSettingsManager.GetFloat("Highscores Settings", "RemoveUsersPercentage", 0.5f);

        if (!_jsonManager.TryReadFromJson(OnHighscoresLoaded, "PlayersScoreData.json"))
        {
            Debug.LogError("Failed to load highscores from json!");
            Highscores = new Highscores { Users = new List<UserData>(_maxUsers) };        
        }

        //Generating user ID space
        _availableIDs = new List<int>(_maxID);
        for (int i = 0; i < _maxID; i++)
        {
            _availableIDs.Add(i);
        }

        foreach (var player in Highscores.Users)
        {
            _availableIDs.Remove(player.ID);
        }
    }

    private void OnHighscoresLoaded(string data)
    {
        Highscores = JsonUtility.FromJson<Highscores>(data);
    }

    public void SaveHighscores() 
    { 
        _jsonManager.TryWriteToJson(Highscores, "PlayersScoreData.json"); 
    }

    public void AddUser()
    {
        //Remove user when users space is filled
        if (_availableIDs.Count == 0 || Highscores.Users.Count == _maxUsers)
        {
            int usersToRemove = (int)(_maxUsers * _removeUsersPercentage);
            int indexToRemove = Highscores.Users.Count - 1;
            int ID;
            for (int i = 0; i < usersToRemove; i++)
            {
                ID = Highscores.Users[i].ID;
                Highscores.Users.RemoveAt(indexToRemove);
                _availableIDs.Add(ID);
                indexToRemove--;
            }
        }

        CurrentUserID = GenerateID();
        Highscores.Users.Add(new UserData { Highscore = 0, ID = CurrentUserID });
    }

    public void UpdateUserHighscore(int ID, int score)
    {
        int playerIndex = GetPlayerIndex(ID);

        if(playerIndex < 0) { return; }

        if (Highscores.Users[playerIndex].Highscore >= score){ return; }

        UserData playerData = new UserData { ID = ID, Highscore = score };
        Highscores.Users.RemoveAt(playerIndex);

        //Add into proper sorted position
        bool wasInserted = false;
        for (int i = 0; i < Highscores.Users.Count; i++)
        {
            if (Highscores.Users[i].Highscore < playerData.Highscore)
            {
                Highscores.Users.Insert(i, playerData);
                wasInserted = true;
                break;
            }
        }

        if (!wasInserted)
        {
            Highscores.Users.Add(playerData);
        }

        Debug.Log("Saving Highscores");
        SaveHighscores();
    }

    public string GetUsername(int ID)
    {
        return $"{_userHeader}{ID:0000}";      
    }
    public int GetHighscore(int ID)
    {
        return Highscores.Users[GetPlayerIndex(ID)].Highscore;
    }
    public int GetPosition(int ID)
    {
        return GetPlayerIndex(ID) + 1;
    }

    public UserData GetUserData(int ID)
    {
        return Highscores.Users[GetPlayerIndex(ID)];
    }

    private int GetPlayerIndex(int ID)
    {
        for (int i = 0; i < Highscores.Users.Count; i++)
        {
            if (Highscores.Users[i].ID == ID) return i;
        }

        Debug.LogWarning("Non Existent ID");
        return -1;
    }

    private int GenerateID()
    {
        int index = UnityEngine.Random.Range(0, _availableIDs.Count);
        int ID = _availableIDs[index];
        _availableIDs.Remove(ID);
        return ID;
    }
}


[Serializable]
public struct Highscores
{
    public List<UserData> Users;
}

[Serializable]
public struct UserData
{
    public int ID;
    public int Highscore;
}