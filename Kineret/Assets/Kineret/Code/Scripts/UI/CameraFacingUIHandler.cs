using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFacingUIHandler : MonoBehaviour
{
    private void Update()
    {
        LookAtCamera();     
    }

    private void LookAtCamera()
    {
        Vector3 cameraPositon = Camera.main.transform.position;
        //Vector3 direction = cameraPositon - transform.position;
        //direction.Normalize();

        transform.LookAt(cameraPositon);
    }
}
