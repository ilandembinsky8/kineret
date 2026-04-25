/*     Unity GIS Tech 2020-2023      */

using System;
using System.Collections.Generic;
using UnityEngine;


namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderGeoPointGenerator
    {
        private static List<GISTerrainLoaderSO_GeoPoint> PointsPrefab;
        public static GISTerrainLoaderPrefs Prefs;
        private static GISTerrainLoaderVectorParameters_SO VectorParameters;

        public static void GenerateGeoPoint(GISTerrainContainer container, List<GISTerrainLoaderPointGeoData> GeoData, GISTerrainLoaderPrefs prefs)
        {
            Prefs = prefs;
            PointsPrefab = Prefs.GeoPointPrefabs;

            VectorParameters = Prefs.VectorParameters_SO;

            GameObject Parent = null;
            if (container.transform.Find("GeoPoints") == null)
            {
                Parent = new GameObject();
                Parent.name = "GeoPoints";
                Parent.transform.parent = container.transform;
            }
            else
                Parent = container.transform.Find("GeoPoints").gameObject;


            for (int i = 0; i < GeoData.Count; i++)
            {
                var GeoDataP = GeoData[i];
                var prefab = GetPointPrefab(GeoDataP.Tag);

                if (prefab != null)
                {
                    if (prefab.Prefabs.Count > 0)
                    {
                        if (prefab.PrefabsInstantiatePlacment == InstantiatePlacment.OneRandomPrefab)
                        {
                            var n = UnityEngine.Random.Range(0, prefab.Prefabs.Count);
                            var Go = prefab.Prefabs[n];

                            if (Go != null)
                                InstantiateGameObject(Go, Parent.transform, GeoDataP, container);

                        }

                        if (prefab.PrefabsInstantiatePlacment == InstantiatePlacment.AllPrefabs)
                        {
                            var PParent = new GameObject();
                            PParent.name = "GeoPoint_" + prefab.GeoPointType + "_" + GeoDataP.Tag;
                            PParent.transform.parent = Parent.transform;
                            PParent.transform.position = GISTerrainLoaderGeoConversion.RealWorldCoordinatesToUnityWorldSpace(container, GeoDataP.GeoPoint);

                            List<GameObject> list_go = new List<GameObject>();

                            foreach (var GO in prefab.Prefabs)
                            {
                                if (GO != null)
                                {
                                    var m_go = InstantiateGameObject(GO, PParent.transform, GeoDataP, container);
                                    list_go.Add(m_go);
                                }
                            }

                            if (prefab.randScale)
                                RandomScale(container, list_go, prefab, PParent.transform);

                            if (prefab.randPos)
                                RandomPos(container, list_go, prefab, PParent.transform);

                            if (prefab.randRot)
                                RandomRot(list_go, prefab);
                        }

                    }
                }


            }

        }

        private static GameObject InstantiateGameObject(GameObject go, Transform Parent, GISTerrainLoaderPointGeoData P, GISTerrainContainer container)
        {
            var GeoPoint = GameObject.Instantiate(go, Parent);
            if (GeoPoint)
            {
                GeoPoint.transform.position = GISTerrainLoaderGeoConversion.RealWorldCoordinatesToUnityWorldSpace(container, P.GeoPoint);
                GeoPoint.name = "GeoPoint_" + P.Tag;
 
                if (VectorParameters.AddVectorDataBaseToGameObjects)
                    GISTerrainLoaderGeoVectorData.AddVectordDataToObject(GeoPoint, VectorObjectType.Point, P, null,null);

                if (GeoPoint.GetComponent<GISTerrainLoaderGeoPoint>())
                    GeoPoint.GetComponent<GISTerrainLoaderGeoPoint>().SetName(P.Name);
            }
            return GeoPoint;
        }

        private static GISTerrainLoaderSO_GeoPoint GetPointPrefab(string pointtype)
        {
            GISTerrainLoaderSO_GeoPoint point = null;
            foreach (var prefab in PointsPrefab)
            {
                if (prefab != null)
                {
                    if (prefab.GeoPointType == pointtype)
                        point = prefab;

                }
            }
            return point;
        }

        private static void RandomPos(GISTerrainContainer container, List<GameObject> list_go, GISTerrainLoaderSO_GeoPoint prefab, Transform Origin)
        {
            int len = list_go.Count;
            for (int idx = 0; idx < len; ++idx)
            {
                if (prefab.posSpace == WorldSpace.World)
                {
                    Vector3 WorldPosition = GetRandomPositionInBox(Origin.transform.position, prefab.minPos, prefab.maxPos);

                    list_go[idx].transform.position = WorldPosition;
                }

                else
                {
                    Vector3 localPosition = GetRandomPositionInBox(Origin.transform.localPosition, prefab.minPos, prefab.maxPos);

                    list_go[idx].transform.position = localPosition;

                }

                SnapToTerrain(list_go[idx].transform);
            }
        }

        private static Vector3 GetRandomPositionInBox(Vector3 center, Vector3 min, Vector3 max)
        {
            // Generate random positions within the box
            float x = center.x + UnityEngine.Random.Range(min.x , max.x );
            float y = center.y + UnityEngine.Random.Range(min.y , max.y );
            float z = center.z + UnityEngine.Random.Range(min.z , max.z );

            return new Vector3(x, y, z);
        }

        private static void RandomRot(List<GameObject> list_go, GISTerrainLoaderSO_GeoPoint prefab)
        {
            int len = list_go.Count;
            for (int idx = 0; idx < len; ++idx)
            {
                Quaternion rot = Quaternion.Euler(UnityEngine.Random.Range(prefab.minRot.x, prefab.maxRot.x),
                                                  UnityEngine.Random.Range(prefab.minRot.y, prefab.maxRot.y),
                                                  UnityEngine.Random.Range(prefab.minRot.z, prefab.maxRot.z));

                if (prefab.rotSpace == WorldSpace.World)
                    list_go[idx].transform.rotation *= rot;
                else
                    list_go[idx].transform.localRotation *= rot;
            }

        }
        private static void RandomScale(GISTerrainContainer container, List<GameObject> list_go, GISTerrainLoaderSO_GeoPoint prefab, Transform Origin)
        {
            int len = list_go.Count;
            for (int idx = 0; idx < len; ++idx)
            {
                Vector3 newScale = new Vector3(UnityEngine.Random.Range(prefab.minScale.x * container.Scale.y, prefab.maxScale.x * container.Scale.y),
                                               UnityEngine.Random.Range(prefab.minScale.y * container.Scale.y, prefab.maxScale.y * container.Scale.y),
                                               UnityEngine.Random.Range(prefab.minScale.z * container.Scale.y, prefab.maxScale.z * container.Scale.y));

                list_go[idx].transform.localScale += newScale;
            }
        }
        private static void SnapToTerrain(Transform Origin)
        {
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(new Vector3(Origin.transform.position.x,5000, Origin.transform.position.z), -Origin.up, out hit, Mathf.Infinity))
                Origin.position = new Vector3(Origin.transform.position.x, hit.point.y, Origin.transform.position.z);

        }


        #region GPX

        public static void GenerateGeoPoint(GISTerrainLoaderGPXFileData m_GPXFileData, GISTerrainContainer container, GameObject GeoPointPrefab)
        {
            GameObject highways = null;

            if (container.transform.Find("GeoPoints") == null)
            {
                highways = new GameObject();
                highways.name = "GeoPoints";
                highways.transform.parent = container.transform;
            }
            else
                highways = container.transform.Find("GeoPoints").gameObject;


            for (int i = 0; i < m_GPXFileData.WayPoints.Count; i++)
            {
                var point = m_GPXFileData.WayPoints[i];

                if (GeoPointPrefab != null)
                {
                    var GeoPoint = GameObject.Instantiate(GeoPointPrefab, highways.transform).GetComponent<GISTerrainLoaderGeoPoint>();

                    if (GeoPoint)
                    {
                        GeoPoint.transform.position = GISTerrainLoaderGeoConversion.RealWorldCoordinatesToUnityWorldSpace(container, new DVector2(point.Longitude, point.Latitude));
                        GeoPoint.name = "GeoPoint_" + point.Name;
                        GeoPoint.SetName(point.Name);
                    }
                }

            }

        }

        #endregion
    }
}

