using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFacingUIHandler : MonoBehaviour
{
    private Camera _camera; 
    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        LookAtCamera();     
    }

    private void LookAtCamera()
    {
        Vector3 oppositeCameraDirection = _camera.transform.forward * -1f;
        //Vector3 direction = cameraPositon - transform.position;
        //direction.Normalize();

        transform.LookAt(oppositeCameraDirection + transform.position);
    }
}
