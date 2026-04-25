using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GISTech.GISTerrainLoader;

public class GPSLocation : MonoBehaviour
{

    public GameObject PlayerPosition;
    public GISTerrainContainer Container;
    public Text UILocation;

    public float latitude;
    public float longitude;
    public float altitude;
    public int gps_count = 0;
    public string message;

    // Start is called before the first frame update
    void Start()
    {
        RuntimeTerrainGenerator.OnFinish += OnTerrainGeneratingCompleted;

        DontDestroyOnLoad(gameObject);
     
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private IEnumerator GetLocation()
    {

            //// First, check if user has location service enabled
            //if (!Input.location.isEnabledByUser)
            //{
            //    UILocation.text = '\n'+("GPS not enabled");
            //    yield break;
            //}

            // Start service before querying location
            Input.location.Start();

            // Wait until service initializes
            int maxWait = 20;

            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait <= 0)
            {
                UILocation.text = '\n' + ("Timed out");

                yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                UILocation.text = '\n' + ("Unable to determine device location");
 
                yield break;
            }

            // Set locational infomations
            while (true)
            {
 
                latitude = Input.location.lastData.latitude;
                longitude = Input.location.lastData.longitude;
                altitude = Input.location.lastData.altitude;
                PlayerPosition.transform.position = GISTerrainLoaderGeoConversion.RealWorldCoordinatesToUnityWorldSpace(Container, new DVector2(longitude, latitude));
                UILocation.text = '\n' + ("Space Position : " + PlayerPosition.transform.position.ToString());
                UILocation.text = '\n' + ("GPS Position : " + latitude +" / " + longitude);

                gps_count++;
                yield return new WaitForSeconds(10);
            }
       

    }

    private void OnTerrainGeneratingCompleted(GISTerrainContainer m_container)
    {
        Container = m_container;
        PlayerPosition.GetComponent<GISTerrainLoaderGeoPoint>().container = m_container;
        
        StartCoroutine(GetLocation());
    }
    private void OnDisable()
    {
        RuntimeTerrainGenerator.OnFinish -= OnTerrainGeneratingCompleted;
    }
}
