using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropellerSpinner : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 1440f;

    private void Update()
    {
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
