/*     Unity GIS Tech 2020-2023      */

using Codice.Client.BaseCommands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderVectorLine : MonoBehaviour
    {
        public VectorDataLineSource LineDataSource;

        public GISTerrainLoaderLineGeoData LineGeoData;

#if GISTechAdvancedVectorGenerator
        public GISTerrainLoaderLineFeature FeatureStyle;
#endif
        public void UpdateGeoDataFrom(GISTerrainContainer Container, CoordinatesSource coordinatesSource = CoordinatesSource.FromGeoDataScript, bool StoreElevationValue = false, RealWorldElevation ElevationMode = RealWorldElevation.Elevation)
        {
            switch (coordinatesSource)
            {

                case CoordinatesSource.FromTerrain:

                    switch (LineDataSource)
                    {
                        case VectorDataLineSource.LineRenderer:
                            //Get the Component Which Conatins Point in Unity Space World
                            LineRenderer lineRenderer = GetComponent<LineRenderer>();
                            List< GISTerrainLoaderPointGeoData > Points = new List<GISTerrainLoaderPointGeoData> ();
                            if (lineRenderer != null)
                            {
                                LineGeoData.GeoPoints.Clear();

                                for (int i = 0; i < lineRenderer.positionCount; i++)
                                {
                                    var point = lineRenderer.GetPosition(i);
                                    var RWPosition = GISTerrainLoaderGeoConversion.UnityWorldSpaceToRealWorldCoordinates(point, Container, StoreElevationValue, ElevationMode);

                                    GISTerrainLoaderPointGeoData GeoPoint = new GISTerrainLoaderPointGeoData();
                                    GeoPoint.GeoPoint = RWPosition.ToDVector2();

                                    if (StoreElevationValue)
                                        GeoPoint.Elevation = (float)RWPosition.z;

                                    Points.Add(GeoPoint);
                                }
                                LineGeoData.GeoPoints.Add(Points);

                            }
                            break;
                        case VectorDataLineSource.Custom:
                            //Add custom code  
                            break;
 
                    }
                     break;
                case CoordinatesSource.FromGeoDataScript:
                    //
                    break;
            }
        }

#if GISTechAdvancedVectorGenerator
        public LineMode lineMode = LineMode.Mesh;

        public float baseWidth = 1f;
        public float zoomScaleFactor = 0.02f;

        private Renderer meshRenderer;
        private LineRenderer lineRenderer;
        private Material materialInstance;

        Camera cam;
        private void Start()
        {
            cam = Camera.main;

            if (lineMode == LineMode.Mesh)
            {
                meshRenderer = GetComponent<Renderer>();
                materialInstance = meshRenderer.material; // use instance
            }
            else if (lineMode == LineMode.LineRenderer)
            {
                lineRenderer = GetComponent<LineRenderer>();
                lineRenderer.useWorldSpace = true;
                lineRenderer.alignment = LineAlignment.View;
                materialInstance = lineRenderer.material; // use instance
            }
 
        }
        private void Update()
        {
            UpdateLineWidth();
        }

        private void UpdateLineWidth()
        {

            if (!cam) return;

            float dynamicWidth = baseWidth;

            if (cam.orthographic)
            {
                dynamicWidth = baseWidth * cam.orthographicSize * zoomScaleFactor;
            }
            else
            {
                float dist = Vector3.Distance(cam.transform.position, transform.position);
                dynamicWidth = baseWidth * dist * zoomScaleFactor;
            }

            if (lineMode == LineMode.LineRenderer)
            {
                lineRenderer.startWidth = dynamicWidth;
                lineRenderer.endWidth = dynamicWidth;
            }
            else
            if (materialInstance != null)
            {
                if (!materialInstance || !cam) return;

                float zoom = cam.orthographic
                    ? cam.orthographicSize
                    : Vector3.Distance(cam.transform.position, transform.position);

            }
            UpdateZoomControlledMaterial(materialInstance, cam);

        }
        public void UpdateZoomControlledMaterial(Material mat, Camera cam)
        {
            // Use orthographic size or camera distance (perspective)
            float zoom = cam.orthographic ? cam.orthographicSize : cam.transform.position.y;

            // Define your scene-specific zoom bounds
            float zoomNear = cam.nearClipPlane;     // usually 0.1
            float zoomFar = cam.farClipPlane;      // e.g., 1000

            // Normalize zoom into [0, 1] (1 = zoomed in, 0 = far away)
            float zoom01 = Mathf.InverseLerp(zoomFar, zoomNear, zoom);
            //materialInstance.SetFloat("_ScreenScale", zoom01);
            // Apply clamped tiling (more repetition when zoomed in)
            float minTileX = 0.01f;
            float maxTileX = 0.1f;
            float tileX = Mathf.Lerp(minTileX, maxTileX, zoom01);

            // Apply clamped line width (thinner when zoomed in)
            float minWidth = baseWidth;
            float maxWidth = baseWidth*10f;
            float lineWidth = Mathf.Lerp(minWidth, maxWidth, 1f - zoom01); // inverse

            // Apply to shader
            mat.SetVector("_Tiling", new Vector4(tileX, 1f, 0f, 0f));
            mat.SetFloat("_WorldLineScale", lineWidth);
        }
        #endif

    }
}