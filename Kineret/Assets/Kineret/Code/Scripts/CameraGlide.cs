using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraGlide : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private Transform[] destinations;
    [SerializeField] private int nextDestination;
    [SerializeField] WaypointPathController line;

    private void Update()
    {
        transform.Translate(Time.deltaTime * speed * transform.forward);
        Vector3 direction = (destinations[nextDestination].position - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime);
        }


        if (speed!=0&&(destinations[nextDestination].position - transform.position).sqrMagnitude < 2000000)
        {
            line.CycleWaypoint();
            if (nextDestination == 1) speed = 0;
            else
                nextDestination = 1;
        }
    }
}
