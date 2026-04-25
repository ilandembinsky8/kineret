using System.Collections;
using UnityEngine;
using GISTech.GISTerrainLoader;
using UnityEngine.UI;

/// <summary>
/// This Tutorial Show How to Load a Vector data without using RuntimeTerrainGenerator 
/// use it if you want to generate road,building ... from vector data 
/// </summary>
public class LoadVectorData : MonoBehaviour
{
    public VectorType vectorType;

    public bool LoadTexture = false;

    public GISTerrainContainer container;
    void Start()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            StartCoroutine(UpdateVecotrData());
    }

    bool UpdateKeyPressed = false;
    // Update is called once per frame
    void Update()
    {
#if UNITY_6000_0_OR_NEWER
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            UpdateKeyPressed = UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
        }
#else
        UpdateKeyPressed = Input.GetKeyDown(KeyCode.Space);
#endif
        if (UpdateKeyPressed)
            StartCoroutine(UpdateVecotrData());
    }
    public IEnumerator UpdateVecotrData()
    {
        GISTerrainLoaderPrefs Prefs = new GISTerrainLoaderPrefs();
        Prefs.LoadSettings();

        Prefs.TerrainFilePath = Application.streamingAssetsPath + "/GIS Terrains/Example_VectorData/Desert.tif";
        Prefs.textureMode = TextureMode.WithTexture;
        ////Set VectorType to OSM 
        Prefs.vectorType = vectorType;
        //Enable Road Generator
        Prefs.EnableVectorGenerator = OptionEnabDisab.Enable;
        Prefs.EnableRoadGeneration = OptionEnabDisab.Enable;
        Prefs.EnableTreeGeneration = OptionEnabDisab.Enable;
        Prefs.EnableBuildingGeneration = OptionEnabDisab.Enable;

#if UNITY_6000_0_OR_NEWER
            if(Prefs.terrainMaterialMode == TerrainMaterialMode.Standard)
            {
                Prefs.terrainMaterialMode = TerrainMaterialMode.Custom;
                Prefs.terrainMaterial = Resources.Load<Material>("Materials/URP");
            }
#endif

        //Call GenerateTextures to Start generating Raster Data
        if (LoadTexture)
        {
            Prefs.textureMode = TextureMode.WithTexture;
            yield return StartCoroutine(container.GenerateTextures(Prefs, true));
        }

        yield return new WaitForSeconds(2f);
        //Call GenerateVectorData to Start generating Vector Data
        yield return StartCoroutine(container.GenerateVectorData(Prefs));
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
    private void OnDisable()
    {
        TreeInstance[] originalTree = new TreeInstance[0];
        foreach (var terrain in container.terrains)
        {
            terrain.terrainData.treeInstances = originalTree;
        }
    }
}
