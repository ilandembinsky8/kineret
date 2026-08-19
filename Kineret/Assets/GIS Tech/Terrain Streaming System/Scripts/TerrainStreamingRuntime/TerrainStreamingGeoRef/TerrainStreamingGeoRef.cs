/*     Unity GIS Tech 2020-2021      */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingGeoRef : MonoBehaviour
    {
        public Text LatLonText;
        public Text ElevationText;
        public Dropdown Projections;
        //Use Mask after adding Terrain to layers list 
        public LayerMask TerrainLayer;

        private DVector2 m_Origin = new DVector2(0, 0);

        private float MinElevation;
        private float MaxElevation;
        private float factor = 1f;
        private float Scale = 1;
        private TerrainStreamingSystemPrefs prefs;

        private Terrain m_terrain;

        private TerrainStreamingContainer container;
        public Terrain terrain
        {
            get { return m_terrain; }
            set
            {
                if (m_terrain != value)
                {
                    m_terrain = value;

                }
            }
        }


        void Start()
        {
            prefs = TerrainStreamingSystemPrefs.Get;

            TerrainStreamingSystem.OnFinish += UpdateOrigin;

            if (Projections)
                Projections.onValueChanged.AddListener(OnProjectionChanged);
        }
        
        /// <summary>
        /// Update terrain Origin for GeoRefrence 
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="minelevation"></param>
        /// <param name="maxelevation"></param>
        private void UpdateOrigin(TerrainStreamingContainer m_container)
        {
            container = m_container;
            if (!container)
                container = GameObject.FindObjectOfType<TerrainStreamingContainer>();

            m_Origin.x = container.UpperLeftCoordinate.x;
            m_Origin.y = container.BottomRightCoordiante.y;

            TerrainStreamingGeoConversion.SetLocalOrigin(new DVector2(container.UpperLeftCoordinate.x,container.BottomRightCoordiante.y));

            MinElevation = container.MinMaxElevation.x;
            MaxElevation = container.MinMaxElevation.y;

            Scale = 100 / container.Scale.y;

        }

        void Update()
        {
            RayCastMousePosition();
        }

        private RaycastHit hitInfo;
        private Ray ray;
        private void RayCastMousePosition()
        {
            hitInfo = new RaycastHit();

            if (Camera.main)
            {
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, Mathf.Infinity, TerrainLayer))
                {
                    if (terrain == null)
                    {
                        terrain = hitInfo.collider.transform.gameObject.GetComponent<Terrain>();
                        ElevationText.text = GetHeight(terrain, hitInfo.point).ToString() + " m ";
                    }


                    if (terrain != null)
                    {
                        if (!string.Equals(hitInfo.collider.transform.name, terrain.name))
                        {
                            terrain = hitInfo.collider.transform.gameObject.GetComponent<Terrain>();
                            ElevationText.text = GetHeight(terrain, hitInfo.point).ToString() + " m ";
                        }
                    }


                    var mousePos = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);

                    if (terrain != null)
                    {
                        ElevationText.text = Math.Round((GetHeight(terrain, hitInfo.point) * factor + MinElevation ),2) + " m ";
                    }

                    if(container)
                    LatLonText.text = GetPosition(mousePos, prefs.Projection);
 
              
                }
            }
 
        }
        public float GetHeight(Terrain terrain, Vector3 position)
        {
            float height = 0;

            if(terrain)
            {
                TerrainData t = terrain.terrainData;
                height = terrain.SampleHeight(position);
               
            }
            return height;
        }
        private string GetPosition(Vector3 SpacePos, Projections proj)
        {
            var LatLon = TerrainStreamingGeoConversion.UnityWorldSpaceToLatLog(SpacePos, container);
            return TerrainStreamingGeoConversion.ConvertLatLonTO(LatLon, proj); ;
        }

        void OnDisable()
        {
            TerrainStreamingSystem.OnFinish -= UpdateOrigin;
        }

        private void OnProjectionChanged(int value)
        {
            var prj = (Projections)value;

            prefs.Projection = prj;
        }
    }
}