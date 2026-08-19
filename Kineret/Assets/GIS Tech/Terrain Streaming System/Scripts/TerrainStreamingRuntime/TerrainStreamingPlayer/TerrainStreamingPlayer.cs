/*     Unity GIS Tech 2020-2021      */


using System.Collections;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingPlayer : MonoBehaviour
    {
        public IntersectionMode intersectionMode = IntersectionMode.FieldOfView;
        public Camera playerCam;

        public Transform PlayerBody;
        public Transform RayCastTransform;


        //InCircle Raduis
        public float m_InCircularRadius = 2;
        public float m_EnvironmentInCircularRadius = 1;
        //Km
        public float InCircularRadius = 2;
        public float EnvironmentInCircularRadius = 1;

        //FOV
        public float ClippingFarDistance;
        public float EnvironmentFOVDistancePercent = 20;


        //ZoneBounds
        public Vector3 m_TerrainLoadSize = new Vector3(1, 1, 1);
        public Vector3 m_EnvironmentLoadSize = new Vector3(1, 1, 1);
        // Km
        public Vector2 TerrainLoadSize = new Vector2(1, 1);
        public Vector2 EnvironmentLoadSize = new Vector2(1, 1);

        private TerrainStreamingSystemPrefs prefs;
        private TerrainStreamingSystem RuntimeGenerator;
        

#if UNITY_EDITOR
        public int lastTab = 0;
#endif

        public void OnClippingFarDistanceChanged(float clippingFarDistance)
        {

        }
        void Start()
        {
            TerrainStreamingSystem.OnFinish += OnStart;

        }
        void OnEnable()
        {
            prefs = TerrainStreamingSystemPrefs.Get;
            RuntimeGenerator = TerrainStreamingSystem.Get;
            GetPositionOnTerrain();
            TerrainStreamingSystem.OnFinish += OnStart;
        }
        void OnDisable()
        {
            TerrainStreamingSystem.OnFinish -= OnStart;
        }
        public void GetPositionOnTerrain()
        {
            var downDirection = Vector3.down;

            RaycastHit hitInfoR;

            var ray = new Ray(RayCastTransform.transform.position, (downDirection));

            Debug.DrawRay(RayCastTransform.transform.position, downDirection * Mathf.Infinity, Color.green);

            if (Physics.Raycast(ray, out hitInfoR, Mathf.Infinity))
            {
                transform.position = new Vector3(transform.position.x, hitInfoR.point.y + 10f, transform.position.z);
            }
        }


        private void OnStart(TerrainStreamingContainer container)
        {

        
        }
        public void SetStartPosition(TerrainStreamingContainer container)
        {
            if (prefs.PlayerStartMode == StartMode.Custom)
            {
                if (prefs.startPosition == null || prefs.startPosition == new DVector2(0, 0) || !container.IncludePoint(prefs.startPosition))
                {
                    prefs.PlayerStartMode = StartMode.Centre;
                    Debug.Log("Player Start Position Null or out of bounds !");

                }
            }


            if (prefs.PlayerStartMode == StartMode.Centre)
            {
                var x_Centre = (container.BottomRightCoordiante.x - container.UpperLeftCoordinate.x) / 2;
                var y_Centre = (container.UpperLeftCoordinate.y - container.BottomRightCoordiante.y) / 2;

                var centreLatLon = new DVector2(container.UpperLeftCoordinate.x + x_Centre, container.UpperLeftCoordinate.y - y_Centre);
                this.transform.position = TerrainStreamingGeoConversion.LatLonToUWS(centreLatLon, container, 2000);
            }

            if (prefs.PlayerStartMode == StartMode.Custom)
            this.transform.position = TerrainStreamingGeoConversion.LatLonToUWS(prefs.startPosition, container,5000);

        }
        public void SetBodyActive(bool show)
        {
            if (show) PlayerBody.gameObject.SetActive(true);
            else
                PlayerBody.gameObject.SetActive(false);
 
        }
        void OnDrawGizmos()
        {
            if (prefs == null)
                prefs = TerrainStreamingSystemPrefs.Get;

            switch (intersectionMode)
            {
                case IntersectionMode.Area:

                    Gizmos.color = Color.yellow;
                    m_TerrainLoadSize = new Vector3(TerrainLoadSize.x * prefs.terrainScale.x * prefs.ScaleFactor, 0, TerrainLoadSize.y * prefs.terrainScale.z * prefs.ScaleFactor);
                    Gizmos.DrawWireCube(transform.position, m_TerrainLoadSize);

                    Gizmos.color = Color.blue;
                    m_EnvironmentLoadSize = new Vector3(EnvironmentLoadSize.x * prefs.terrainScale.x * prefs.ScaleFactor, 0, EnvironmentLoadSize.y * prefs.terrainScale.z * prefs.ScaleFactor);
                    Gizmos.DrawWireCube(transform.position, m_EnvironmentLoadSize);

                    break;
                case IntersectionMode.FieldOfView:

                    Gizmos.color = Color.yellow;
                    Gizmos.matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, Vector3.one);
                    if(playerCam)
                    Gizmos.DrawFrustum(Vector3.zero, playerCam.fieldOfView, ClippingFarDistance, playerCam.nearClipPlane, playerCam.aspect);

                    break;
                case IntersectionMode.InCircular:

                    m_InCircularRadius = InCircularRadius * prefs.terrainScale.x * prefs.ScaleFactor;
                    DrawCircle(this.transform.position, m_InCircularRadius, Color.yellow);

                    m_EnvironmentInCircularRadius = EnvironmentInCircularRadius * prefs.terrainScale.x * prefs.ScaleFactor;
                    DrawCircle(this.transform.position, m_EnvironmentInCircularRadius, Color.blue);

                    break;
            }
        }
        void DrawCircle(Vector3 PlayerPos, float Raduis, Color color)
        {
            int segments = 15;

            Gizmos.color = color;
            float _x, _y, z = 0;
            float angle = 0;
            float angleSetep = (360 / segments);

            Vector3 lastpoint = new Vector3();
            for (int i = 0; i < segments + 1; i++)
            {
                _x = Mathf.Sin(Mathf.Deg2Rad * angle) * Raduis + PlayerPos.x;
                _y = Mathf.Cos(Mathf.Deg2Rad * angle) * Raduis + PlayerPos.z;

                z = PlayerPos.y;
                angle += angleSetep;

                if (i > 0)
                    Gizmos.DrawLine(lastpoint, new Vector3(_x, z, _y));

                lastpoint = new Vector3(_x, z, _y);
            }
        }
        public void CheckFall(TerrainStreamingContainer container, float RealWorldElevation)
        {
            if (container)
            {
                var terrain = TerrainStreamingGeoConversion.GetTerrain(this.transform.position);

                if (!terrain)
                {
                    var elevation = TerrainStreamingGeoConversion.GetHeight(this.transform.position) + RealWorldElevation / 10 * container.Scale.y;

                    var pos = new Vector3(this.transform.position.x, elevation + 20, this.transform.position.z);

                    this.transform.position = pos;
                }

            }

        }

    }

}