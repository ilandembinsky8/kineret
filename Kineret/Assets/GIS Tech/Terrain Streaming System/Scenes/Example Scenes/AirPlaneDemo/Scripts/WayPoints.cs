using GISTech.TerrainStreaming;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPoints : MonoBehaviour
{
    // (Lat-Lon-Elevation(m))
    public List<DVector3> RealWorldPoints = new List<DVector3>();
    // (Space Positions)
    [HideInInspector]
    public List<Vector3> UnityWorldSpacePoints = new List<Vector3>();

    public void GenerateRandomWayPoints(TerrainStreamingContainer container,int RandomWayPointsNumber, bool InstantiateGameObjects)
    {
        if(container && RandomWayPointsNumber>0)
        {
            UnityWorldSpacePoints = new List<Vector3>();
            RealWorldPoints = new List<DVector3>();
            this.transform.DestroyChildren();

            for (int i =0; i< RandomWayPointsNumber;i++)
            {
                double p_x = UnityEngine.Random.Range((float)container.UpperLeftCoordinate.x, (float)container.BottomRightCoordiante.x);
                double p_y = UnityEngine.Random.Range((float)container.BottomRightCoordiante.y, (float)container.UpperLeftCoordinate.y);
                float  p_e = UnityEngine.Random.Range(((container.MinMaxElevation.y - container.MinMaxElevation.x)/2)-50 , ((container.MinMaxElevation.y - container.MinMaxElevation.x) / 2) + 50);
                DVector3 point = new DVector3(p_x, p_y, p_e);
                RealWorldPoints.Add(point);

                var spaceP = TerrainStreamingGeoConversion.LatLonToUWS(new DVector2(point.x, point.y), container, (float)point.z);
                UnityWorldSpacePoints.Add(spaceP);

                if (InstantiateGameObjects)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    p.name = "Point_" + RealWorldPoints.IndexOf(point).ToString();
                    p.transform.position = spaceP;
                    p.transform.parent = this.transform;
                }
            }
        }
    }
    public void ConvertLatLonToSpacePosition(TerrainStreamingContainer terrainContainer, bool InstantiateGameObjects)
    {
        UnityWorldSpacePoints = new List<Vector3>();

        this.transform.DestroyChildren();

        foreach (var point in RealWorldPoints)
        {
            var spaceP = TerrainStreamingGeoConversion.LatLonToUWS(new DVector2(point.x, point.y), terrainContainer, (float)point.z);

            UnityWorldSpacePoints.Add(spaceP);

            if (InstantiateGameObjects)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.name = "Point_" + RealWorldPoints.IndexOf(point).ToString();
                p.transform.position = spaceP;
                p.transform.parent = this.transform;
            }
        }

    }

}
