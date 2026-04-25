using System.Collections;
using UnityEngine;
using GISTech.GISTerrainLoader;

/// <summary>
/// This Tutorial Show How to texture a terrain at runtime without using the RuntimeTerrainGenerator 
/// use it  if you want to generate splatmaps or shaded relief and come back to real world texture ... verso
/// </summary>
public class UpdateTextures : MonoBehaviour
{
    public TextureMode texturemode;

    public GISTerrainContainer container;
    // Start is called before the first frame update
    void Start()
    {

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
            StartCoroutine(UpdateRasterData());
    }


    public IEnumerator UpdateRasterData()
    {

        GISTerrainLoaderPrefs Prefs = new GISTerrainLoaderPrefs();
        //Load GTL Settings
        Prefs.LoadSettings();

        Prefs.TerrainFilePath = Application.streamingAssetsPath + "/GIS Terrains/UTM-NAD83/Tiff.tif";
        ////Set TextureMode to With Texture
        Prefs.textureMode = texturemode;
        Prefs.TerrainShaderType = ShaderType.ColorRamp;
        Prefs.UnderWaterShader = OptionEnabDisab.Disable;

#if UNITY_6000_0_OR_NEWER
            if(Prefs.terrainMaterialMode == TerrainMaterialMode.Standard)
            {
                Prefs.terrainMaterialMode = TerrainMaterialMode.Custom;
                Prefs.terrainMaterial = Resources.Load<Material>("Materials/URP");
            }
#endif
        //Call GenerateTextures to Start generating Raster Data
        yield return StartCoroutine(container.GenerateTextures(Prefs, true));


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
