using GISTech.TerrainStreaming;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Airplane : MonoBehaviour
{
    KeyCode MotorSpeedUp = KeyCode.Z;
    KeyCode MotorSpeedDown = KeyCode.S;

    public float Movingspeed;
    public float RotationSpeed;
    // Real World Waypoints In Lat-Lon-Elevation
    public WayPoints waypoints;

    [HideInInspector]
    public Vector3 TargetPoint = new Vector3(0, 0, 0);
    private Quaternion rotation;
    private int wayIndex = 0;

    [HideInInspector]
    public bool EnableFlying;

    private TerrainStreamingContainer container;
    private TerrainStreamingSystem RuntimeGenerator;


    public float ProppelerRotSpeed = 10;
    

    [Header("Manual Flight")]
    public bool ManualControl = true;
    public KeyCode ToggleManualKey = KeyCode.M;
    public float ManualPitchSpeed = 35f;
    public float ManualYawSpeed = 25f;
    public float ManualRollSpeed = 45f;
    public float ManualMinSpeed = 0f;
    public float ManualMaxSpeed = 3000f;
    public float ManualThrottleAcceleration = 300f;
    public float ManualVerticalSpeed = 5000f;
    public float ManualVerticalBoostMultiplier = 4f;
    public KeyCode ManualVerticalBoostKey = KeyCode.RightShift;
public Transform ProppelerModel;
 
    public void OnTerrainGeneratingCompleted()
    {
        RuntimeGenerator = TerrainStreamingSystem.Get;
        container = TerrainStreamingSystem.Get.SectorContainer;
        if (waypoints.UnityWorldSpacePoints.Count > 0)
            TargetPoint = waypoints.UnityWorldSpacePoints[0];

    }
void Update()
    {
        if (ProppelerModel != null)
            ProppelerModel.transform.localRotation = Quaternion.Euler(0, 0, ProppelerRotSpeed * Time.deltaTime * 10000);

        if (Input.GetKeyDown(ToggleManualKey))
        {
            ManualControl = !ManualControl;
            Debug.Log("Airplane control mode: " + (ManualControl ? "MANUAL" : "AUTO WAYPOINTS"));
        }

        if (ManualControl)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                Movingspeed = Mathf.MoveTowards(Movingspeed, ManualMaxSpeed, ManualThrottleAcceleration * Time.deltaTime);

            if (Input.GetKey(KeyCode.LeftControl))
                Movingspeed = Mathf.MoveTowards(Movingspeed, ManualMinSpeed, ManualThrottleAcceleration * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.T))
            {
                var euler = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0, euler.y, 0);
            }
        }
        else
        {
            if (Input.GetKey(MotorSpeedUp) && Movingspeed < 70)
                Movingspeed += 0.01f;

            if (Input.GetKey(MotorSpeedDown) && Movingspeed > 6)
                Movingspeed -= 0.01f;
        }
    }

private void FixedUpdate()
    {
        if (ManualControl)
        {
            ManualFlight();
            return;
        }

        if (TargetPoint != Vector3.zero)
        {
            AirPlaneGuidance(TargetPoint);

            var dis = Vector3.Distance(transform.position, TargetPoint);

            if (dis < 10f)
            {
                wayIndex++;
                if (wayIndex > waypoints.UnityWorldSpacePoints.Count - 1)
                    wayIndex = 0;
                TargetPoint = waypoints.UnityWorldSpacePoints[wayIndex];
                return;
            }
        }
    }

private void ManualFlight()
    {
        float dt = Time.fixedDeltaTime;

        float pitchInput = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        float yawInput = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        float rollInput = (Input.GetKey(KeyCode.A) ? 1f : 0f) - (Input.GetKey(KeyCode.D) ? 1f : 0f);

        transform.Rotate(
            -pitchInput * ManualPitchSpeed * dt,
            yawInput * ManualYawSpeed * dt,
            rollInput * ManualRollSpeed * dt,
            Space.Self);

        float verticalInput = (Input.GetKey(KeyCode.PageUp) ? 1f : 0f) - (Input.GetKey(KeyCode.PageDown) ? 1f : 0f);
        float verticalBoost = Input.GetKey(ManualVerticalBoostKey) ? ManualVerticalBoostMultiplier : 1f;
        transform.position += Vector3.up * verticalInput * ManualVerticalSpeed * verticalBoost * dt;

        transform.position += transform.forward * Movingspeed * dt;
    }

    private void AirPlaneGuidance(Vector3 target)
    {
        Vector3 relPos = target - transform.position;
        relPos = target - transform.position;
        rotation = Quaternion.LookRotation(relPos, transform.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, RotationSpeed * Time.fixedDeltaTime);
        transform.Translate(transform.forward * Movingspeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Get the real world Elevation of the airplane 
    /// </summary>
    /// <returns></returns>
    public DVector3 GetAirPlaneLatLonElevation()
    {
        var LatLonPos = TerrainStreamingGeoConversion.UnityWorldSpaceToLatLog(this.transform.position, container);

        return new DVector3(LatLonPos.x, LatLonPos.y, Math.Round(TerrainStreamingGeoConversion.GetRealWorldHeight(container, this.transform.position), 2));
    }
    /// <summary>
    /// Get the the Current WayPoint LatLon
    /// </summary>
    /// <returns></returns>
    public DVector2 GetWayPointLatLon()
    {
        var LatLonPos = TerrainStreamingGeoConversion.UnityWorldSpaceToLatLog(TargetPoint, container);

        return LatLonPos;
    }
}