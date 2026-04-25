using GISTech.GISTerrainLoader;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ShowBuildingDataBase : MonoBehaviour
{
    public Camera mainCamera; // Assign your main camera here
    public Text infoText;     // UI Text element to display the data

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var polygon = hit.collider.GetComponent<GISTerrainLoaderVectorPolygon>();

                if (polygon != null)
                    DisplayPolygonData(polygon);
            }
        }
    }

    void DisplayPolygonData(GISTerrainLoaderVectorPolygon polygon)
    {
        if (polygon.PolygonGeoData.DataBase == null) return;

        StringBuilder sb = new StringBuilder();

        foreach (var kvp in polygon.PolygonGeoData.DataBase)
            sb.AppendLine($"{kvp.Key}: {kvp.Value}");


        infoText.text = sb.ToString();
    }
}
