using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerHUDHandler : MonoBehaviour
{
    [SerializeField] private TransformEventChannel cameraPitched_EC;
    [SerializeField] private TransformEventChannel playerMoved_EC;
    [SerializeField] private BoolEventChannel gamePause_EC;

    [SerializeField] private TMP_Text altitudeText;
    [SerializeField] private TMP_Text higherPitchText;
    [SerializeField] private TMP_Text currentPitchText;
    [SerializeField] private TMP_Text lowerPitchText;

    [SerializeField] private TMP_Text timerText;

    [SerializeField] private string scoreTitleText;
    [SerializeField] private TMP_Text scoreText;
    private int _time;
    private bool _isPaused;

    private void Start()
    {
        StartCoroutine(StartTimer());
    }

    private void OnEnable()
    {
        playerMoved_EC.OnEventRaised += HandlePlayerMoved;
        cameraPitched_EC.OnEventRaised += HandleCameraPitched;
        gamePause_EC.OnEventRaised += HandleGamePause;
    }

    private void OnDisable()
    {
        playerMoved_EC.OnEventRaised -= HandlePlayerMoved;
        cameraPitched_EC.OnEventRaised -= HandleCameraPitched;
        gamePause_EC.OnEventRaised -= HandleGamePause;
    }
    private void HandleCameraPitched(Transform cameraTransform)
    {
        int pitch = Mathf.FloorToInt(cameraTransform.localEulerAngles.x);

        if (cameraTransform.up.y >= 0)
        {
            if(pitch > 90) pitch = 360 - pitch;
            else pitch *= -1; 
        }
        else
        {
            pitch -= 180;
        }


            currentPitchText.text = pitch.ToString();
        higherPitchText.text = (pitch + 1).ToString();
        lowerPitchText.text = (pitch - 1).ToString();
    }

    private void HandlePlayerMoved(Transform playerTransform)
    {
        int altitude = Mathf.FloorToInt(playerTransform.position.y);
        altitudeText.text = altitude.ToString();
    }

    private void HandleGamePause(bool isPaused)
    {
        _isPaused = isPaused;
    }

    private void UpdateTimerUI()
    {
        if (_isPaused) return;

        _time++;
        int seconds = _time % 60;
        int minutes = _time / 60;
        timerText.text = string.Format("{0:00}", minutes) + ":" +  string.Format("{0:00}", seconds);
    }
    private IEnumerator StartTimer()
    {
        while (true)
        {
            UpdateTimerUI();
            yield return new WaitForSeconds(1);         
        }      
    }
}
