using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

public class IdleManager : MonoBehaviour
{
    public static bool IsTicking = false;

    private float _timer;
    private int _allowedIdleTime;

    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _allowedIdleTime = GameSettingsManager.GetInt("Game Settings", "MaxIdleDurationInSeconds", 600);
        InputSystem.onAnyButtonPress.Call(ctrl => _timer = 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsTicking)
        {
            _timer += Time.deltaTime;
            if (_allowedIdleTime <= _timer)
            {
                ResetGame();
            }
        }       
    }

    private void ResetGame()
    {
        //Do stuff relating to user management
        _timer = 0;
        IsTicking = false;
        MainMenuManager.IsLoadingDestinationSelection = false;
        SceneManager.LoadScene("Main Menu Scene");
    }
}
