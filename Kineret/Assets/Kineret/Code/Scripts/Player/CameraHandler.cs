using UnityEngine;
using Cinemachine;
using System;

public class CameraHandler : MonoBehaviour
{
    public static Action<bool> OnAnyCameraChangedIsFirstPerson;

    [SerializeField] private CinemachineBrain _cinemachineBrain;
    [SerializeField] private CinemachineVirtualCamera _1stPersonCamera;
    [SerializeField] private CinemachineVirtualCamera _3rdPersonCamera;

    private float _currentCamDistanceValue;
    private float _distanceValueToSwap;
    private float _minCamDistance = -1f;
    private float _maxCamDistance = 1f;
    private float _customInputDeadzone = 0.01f;

    private void Update() { MoveCam(); }

    private void MoveCam()
    {
        float _roughVertical = Input.GetAxis(JoystickManager.JoystickControls.RoughVerticalAxis);
        //Debug.Log($"<color=orange>Wanted Directions: FH: {_flatHorizontal}, FV: {_flatVertical}, RH: {_roughHorizontal}, RV: {_roughVertical}</color>");

        if (Mathf.Abs(_roughVertical) > _customInputDeadzone)
        {
            if (_roughVertical < 0f)
                _currentCamDistanceValue = _minCamDistance;
            else
                _currentCamDistanceValue = _maxCamDistance;

            HandleCamValueChanged();
        }

    }
    private void HandleCamValueChanged()
    {
        //Debug.Log("<color=cyan>Current Cam Distance Value: " + _currentCamDistanceValue + "</color>");

        if (_currentCamDistanceValue <= _distanceValueToSwap) // Pulling down trigger for 3rd person camera
        {
            _1stPersonCamera.gameObject.SetActive(false);
            _3rdPersonCamera.gameObject.SetActive(true);
            OnAnyCameraChangedIsFirstPerson?.Invoke(false);
        }
        else // Pushing up trigger for 1st person camera
        {
            _1stPersonCamera.gameObject.SetActive(true);
            _3rdPersonCamera.gameObject.SetActive(false);
            OnAnyCameraChangedIsFirstPerson?.Invoke(true);
        }
    }

}