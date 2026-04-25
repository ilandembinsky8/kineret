using GISTech.GISTerrainLoader;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

public class exportTerrain : MonoBehaviour
{
    public string TerrainFilePath;
    public string ExportTerrainFilePath;
    private RuntimeTerrainGenerator RuntimeGenerator;

    private GISTerrainLoaderPrefs Prefs;
    private GISTerrainLoaderRuntimePrefs RuntimePrefs;
 
    // Start is called before the first frame update
    void Start()
    {
        RuntimePrefs = GISTerrainLoaderRuntimePrefs.Get;
        RuntimeGenerator = RuntimeTerrainGenerator.Get;
        Prefs = RuntimePrefs.Prefs;
        Prefs.TerrainFilePath = Application.streamingAssetsPath + TerrainFilePath;

        StartCoroutine(GenerateTerrain(Prefs.TerrainFilePath));
    }
    private IEnumerator GenerateTerrain(string TerrainPath)
    {
        yield return new WaitForSeconds(2f);

        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            if (string.IsNullOrEmpty(TerrainPath) || !System.IO.File.Exists(TerrainPath))
            {

                Debug.LogError("Terrain file null or not supported.. Try againe");
                yield break;
            }
        }

        InitializingRuntimePrefs(TerrainPath);
        StartCoroutine(RuntimeGenerator.StartGenerating(Prefs));

    }
    private void InitializingRuntimePrefs(string TerrainPath)
    {
        RuntimeGenerator.enabled = true;
        Prefs.RemovePrvTerrain = OptionEnabDisab.Enable;
        Prefs.projectionMode = ProjectionMode.AutoDetection;

        ////Load Real Terrain elevation values
        Prefs.TerrainElevation = TerrainElevation.RealWorldElevation;

        Prefs.terrainDimensionMode = TerrainDimensionsMode.AutoDetection;

        Prefs.heightmapResolution = 257;


        Prefs.textureMode = TextureMode.WithoutTexture;
        Prefs.terrainCount = new Vector2Int(2,2);

        ////if terrain has water areas and you want to export them to DEM, you should enable this option to get the correct elevation values for water areas, otherwise they will be exported as flat areas with the same elevation value which is not correct.
        Prefs.UnderWater = OptionEnabDisab.Enable;


#if UNITY_6000_0_OR_NEWER
            if(Prefs.terrainMaterialMode == TerrainMaterialMode.Standard)
            {
                Prefs.terrainMaterialMode = TerrainMaterialMode.Custom;
                Prefs.terrainMaterial = Resources.Load<Material>("Materials/URP");
            }
#endif
    }
    private void Awake()
    {
#if UNITY_6000_0_OR_NEWER
            //Fix Input system conflict with new Unity versions
            UnityEngine.EventSystems.EventSystem es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
            var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null) Destroy(standalone);
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
    }

    public void ExportTerrainToDEM()
    {
        if(RuntimeGenerator.GeneratedContainer !=null)
        {
            GISTerrainLoaderTiffExporter TiffExporter = new GISTerrainLoaderTiffExporter(Application.streamingAssetsPath + ExportTerrainFilePath, RuntimeGenerator.GeneratedContainer);
            TiffExporter.ExportToTiff();
        }

    }
 
}
