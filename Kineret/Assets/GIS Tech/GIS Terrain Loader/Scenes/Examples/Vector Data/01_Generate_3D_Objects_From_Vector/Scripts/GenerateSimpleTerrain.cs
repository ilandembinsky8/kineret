using System.Collections;
using UnityEngine;
using GISTech.GISTerrainLoader;
public class GenerateSimpleTerrain : MonoBehaviour
{
    private string TerrainFilePath;

    private RuntimeTerrainGenerator RuntimeGenerator;

    private GISTerrainLoaderPrefs Prefs;
    private GISTerrainLoaderRuntimePrefs RuntimePrefs;

    void Start()
    {
        TerrainFilePath = Application.streamingAssetsPath + "/GIS Terrains/Example_SHP/Cuenca.tif";

        RuntimePrefs = GISTerrainLoaderRuntimePrefs.Get;
        Prefs = RuntimePrefs.Prefs;

        RuntimeGenerator = RuntimeTerrainGenerator.Get;
 
        StartCoroutine(GenerateTerrain(TerrainFilePath));
    }
    private IEnumerator GenerateTerrain(string TerrainPath)
    {
        yield return new WaitForSeconds(2f);
 
            if (Application.platform == RuntimePlatform.WebGLPlayer || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {

            InitializingRuntimePrefs(TerrainPath);
            StartCoroutine(RuntimeGenerator.StartGenerating(Prefs));
        }else
        {
            if (!string.IsNullOrEmpty(TerrainPath) && System.IO.File.Exists(TerrainPath))
            {
                InitializingRuntimePrefs(TerrainPath);

                StartCoroutine(RuntimeGenerator.StartGenerating(Prefs));
            }
            else
            {
                Debug.LogError("Terrain file null or not supported.. Try againe");
                yield return null;
            }
        }

    }
    private void InitializingRuntimePrefs(string TerrainPath)
    {
        RuntimeGenerator.enabled = true;
        Prefs.TerrainFilePath = TerrainPath;
        Prefs.RemovePrvTerrain =  OptionEnabDisab.Enable;

        //Load Real Terrain elevation values
        Prefs.TerrainElevation = TerrainElevation.RealWorldElevation;
        Prefs.terrainDimensionMode = TerrainDimensionsMode.AutoDetection;
        Prefs.heightmapResolution = 65;
        Prefs.textureloadingMode = TexturesLoadingMode.AutoDetection;
        Prefs.terrainMaterialMode = TerrainMaterialMode.Standard;


        Prefs.EnableVectorGenerator = OptionEnabDisab.Enable;

        Prefs.vectorType = VectorType.OpenStreetMap;
        Prefs.EnableRoadGeneration = OptionEnabDisab.Enable;
        Prefs.EnableBuildingGeneration = OptionEnabDisab.Enable;
        Prefs.EnableTreeGeneration = OptionEnabDisab.Enable;
        Prefs.BillBoardStartDistance= 1000f;


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


}
