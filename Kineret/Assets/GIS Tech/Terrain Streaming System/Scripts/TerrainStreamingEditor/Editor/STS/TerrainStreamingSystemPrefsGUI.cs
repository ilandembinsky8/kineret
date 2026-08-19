/*     Unity GIS Tech 2020-2021      */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    [CustomEditor(typeof(TerrainStreamingSystemPrefs))]
    public class TerrainStreamingSystemPrefsGUI : Editor
    {
        private TerrainStreamingSystemPrefs RuntimePrefs { get { return target as TerrainStreamingSystemPrefs; } }

        private TabsBlock tabs;

        private Texture2D m_resetPrefs;

        [MenuItem("Tools/GIS Tech/Terrain Streaming/Add Runtime TSS to Scene", false, 2)]
        public static void AddRuntimeGTLToScene()
        {
            if (!GameObject.FindObjectOfType<TerrainStreamingSystem>())
            {
                var GISTech = new GameObject("GIS Tech");
                var RuntimeTSS = new GameObject("Runtime Terrain Streaming");
                RuntimeTSS.transform.parent = GISTech.transform;
                RuntimeTSS.gameObject.AddComponent<TerrainStreamingSystemPrefs>();
                RuntimeTSS.gameObject.AddComponent<TerrainStreamingSystem>();
            }
            else
            {
                Debug.LogError("Runtime Terrain Streaming already exists in your scene");
            }

        }
        private void OnEnable()
        {
            tabs = new TabsBlock(new Dictionary<string, System.Action>()
            {
                {"Player", PlayerTab},
                {"Elevation,Scaling..", ElevationScalingTab},
                {"Terrain Preferences", TerrainPreferencesTab},
                {"Environment Preferences", EnvironmentOptionsTab}
            });
            tabs.SetCurrentMethod(RuntimePrefs.lastTab);

            if (m_resetPrefs == null)
                m_resetPrefs = LoadTexture("GTL_ResetPrefs");
        }


        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            Undo.RecordObject(RuntimePrefs, "TSS_Runtime");
            tabs.Draw();
            if (GUI.changed)
                RuntimePrefs.lastTab = tabs.curMethodIndex;
            EditorUtility.SetDirty(RuntimePrefs);
        }
        private void PlayerTab()
        {
            using (new VerticalBlock(GUI.skin.box))
            {
                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Player Start Position ", " Position from where the player will be located at the start / Centre : Terrain Centre Position / Custom : Set Lat/Lon Position"), GUILayout.MaxWidth(200));
                        RuntimePrefs.PlayerStartMode = (StartMode)EditorGUILayout.EnumPopup("", RuntimePrefs.PlayerStartMode, GUILayout.ExpandWidth(true));
                    }

                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        if (RuntimePrefs.PlayerStartMode == StartMode.Custom)
                        {
                            GUILayout.Label(new GUIContent(" Latitude/longitude     ", "Player Start Position in lat/Lon "), GUILayout.MaxWidth(250));
                            RuntimePrefs.startPosition.y = EditorGUILayout.DoubleField(RuntimePrefs.startPosition.y, GUILayout.ExpandWidth(true));
                            RuntimePrefs.startPosition.x = EditorGUILayout.DoubleField(RuntimePrefs.startPosition.x, GUILayout.ExpandWidth(true));
                        }

                    }
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Player Ref ", " Refernce to the Player Script"), GUILayout.MaxWidth(200));
                        RuntimePrefs.player = (TerrainStreamingPlayer)EditorGUILayout.ObjectField(RuntimePrefs.player,typeof(TerrainStreamingPlayer),true);
                    }
                }

            }

          

        }
        private void ElevationScalingTab()
        {
            using (new VerticalBlock(GUI.skin.box))
            {
                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Terrain Scale ", " Specifies the terrain scale factor in three directions (if terrain is large with 1 value you can set small float value like 0.5f - 0.1f - 0.01f"), GUILayout.MaxWidth(200));
                        RuntimePrefs.terrainScale = EditorGUILayout.Vector3Field("", RuntimePrefs.terrainScale);
                    }

                }

            }


        }
        private void TerrainPreferencesTab()
        {

            using (new VerticalBlock(GUI.skin.box))
            {
                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.button))
                    {
                        GUILayout.Label("Terrain Base prefs ");
                    }

                    using (new VerticalBlock(GUI.skin.box))
                    {

                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Detail Resolution ", "The number of cells available for placing details onto the Terrain tile used to controls grass and detail meshes. Lower you set this number performance will be better"), GUILayout.MaxWidth(200));
                            RuntimePrefs.detailResolution_index = EditorGUILayout.Popup(RuntimePrefs.detailResolution_index, RuntimePrefs.availableHeightSrt, GUILayout.ExpandWidth(true));
                            RuntimePrefs.detailResolution = RuntimePrefs.availableHeights[RuntimePrefs.detailResolution_index];

                        }
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Base Map Resolution ", "Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance"), GUILayout.MaxWidth(200));
                            RuntimePrefs.baseMapResolution_index = EditorGUILayout.Popup(RuntimePrefs.baseMapResolution_index, RuntimePrefs.availableHeightSrt, GUILayout.ExpandWidth(true));
                            RuntimePrefs.baseMapResolution = RuntimePrefs.availableHeights[RuntimePrefs.baseMapResolution_index];
                        }
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Resolution Per Patch ", "The number of cells in a single patch (mesh), recommended value is 16 for very large detail object distance "), GUILayout.MaxWidth(200));
                            RuntimePrefs.resolutionPerPatch_index = EditorGUILayout.Popup(RuntimePrefs.resolutionPerPatch_index, RuntimePrefs.availableHeightsResolutionPrePectSrt, GUILayout.ExpandWidth(true));
                            RuntimePrefs.resolutionPerPatch = RuntimePrefs.availableHeightsResolutionPrePec[RuntimePrefs.resolutionPerPatch_index];
                        }
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Pixel Error ", " The accuracy of the mapping between Terrain maps (such as heightmaps and Textures) and generated Terrain. Higher values indicate lower accuracy, but with lower rendering overhead. "), GUILayout.MaxWidth(200));
                            RuntimePrefs.PixelErro = EditorGUILayout.Slider(RuntimePrefs.PixelErro, 1, 200, GUILayout.ExpandWidth(true));
                        }

                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Base Map Dis ", " The maximum distance at which Unity displays Terrain Textures at full resolution. Beyond this distance, the system uses a lower resolution composite image for efficiency "), GUILayout.MaxWidth(200));
                            RuntimePrefs.BaseMapDistance = EditorGUILayout.Slider(RuntimePrefs.BaseMapDistance, 1, 20000, GUILayout.ExpandWidth(true));
                        }

                    }
                }
                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("Tree & Details objects ");
                }
                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent("  Detail Distance ", " The distance from the camera beyond which details are culled "), GUILayout.MaxWidth(200));
                        RuntimePrefs.DetailDistance = EditorGUILayout.Slider(RuntimePrefs.DetailDistance, 10f, 400, GUILayout.ExpandWidth(true));
                    }

                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent("  Detail Density ", " The number of detail/grass objects in a given unit of area. Set this value lower to reduce rendering overhead "), GUILayout.MaxWidth(200));
                        RuntimePrefs.DetailDensity = EditorGUILayout.Slider(RuntimePrefs.DetailDensity, 0, 1, GUILayout.ExpandWidth(true));
                    }
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent("  Tree Distance ", " The distance from the camera beyond which trees are culled "), GUILayout.MaxWidth(200));
                        RuntimePrefs.TreeDistance = EditorGUILayout.Slider(RuntimePrefs.TreeDistance, 1, 5000, GUILayout.ExpandWidth(true));
                    }
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent("  Tree BillBoard Start Distance ", "The distance from the camera at which Billboard images replace 3D Tree objects"), GUILayout.MaxWidth(200));
                        RuntimePrefs.BillBoardStartDistance = EditorGUILayout.Slider(RuntimePrefs.BillBoardStartDistance, 1, 2000, GUILayout.ExpandWidth(true));
                    }
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent("  Fade Length ", "The distance over which Trees transition between 3D objects and Billboards."), GUILayout.MaxWidth(200));
                        RuntimePrefs.FadeLength = EditorGUILayout.Slider(RuntimePrefs.FadeLength, 1, 200, GUILayout.ExpandWidth(true));
                    }
                }

                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("Terrain Texturing ");
                }

                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Generate Textures ", " Enable/Disable textures generation "), GUILayout.MaxWidth(200));
                        RuntimePrefs.GenerateTextures = (OptionEnabDisab)EditorGUILayout.EnumPopup("", RuntimePrefs.GenerateTextures);
                    }

                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Terrain Material Mode ", "This option used to cutomize terrain material ex : in case of using HDRP "), GUILayout.MaxWidth(200));
                        RuntimePrefs.terrainMaterialMode = (TerrainMaterialMode)EditorGUILayout.EnumPopup("", RuntimePrefs.terrainMaterialMode, GUILayout.ExpandWidth(true));
                    }
                    if (RuntimePrefs.terrainMaterialMode == TerrainMaterialMode.Custom)
                    {

                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent("  Terrain Material ", "Materail that will be used in the generated terrains "), GUILayout.MaxWidth(200));
                            RuntimePrefs.terrainMaterial = (Material)EditorGUILayout.ObjectField(RuntimePrefs.terrainMaterial, typeof(Material), true, GUILayout.ExpandWidth(true));
                        }
                    }
                }


                using (new HorizontalBlock(GUI.skin.button))
                {
                    GUILayout.Label("Horzion Terrain");
                }

                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Generate Horizon Terrain ", " Enable/Disable Horizon generation "), GUILayout.MaxWidth(200));
                        RuntimePrefs.GenerateHorizon = (OptionEnabDisab)EditorGUILayout.EnumPopup("", RuntimePrefs.GenerateHorizon);
                    }

                    if(RuntimePrefs.GenerateHorizon == OptionEnabDisab.Enable)
                    {
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Horizon Y Offest ", " Y Offest Value of Horizon Terrain to be under the streamed terrain"), GUILayout.MaxWidth(200));
                            RuntimePrefs.HorizonYOffest = EditorGUILayout.Slider(RuntimePrefs.HorizonYOffest, -50, 0, GUILayout.ExpandWidth(true));
                        }
                    }


                }
            }
        }
        private void EnvironmentOptionsTab()
        {
            using (new VerticalBlock(GUI.skin.box))
            {
                using (new VerticalBlock(GUI.skin.box))
                {
                    using (new HorizontalBlock(GUI.skin.button))
                    {
                        GUILayout.Label(" Trees");
                    }
                    using (new VerticalBlock(GUI.skin.box))
                    {
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Generate Trees ", " Enable/Disable to generate terrain trees "), GUILayout.MaxWidth(200));
                            RuntimePrefs.GenerateTrees = (OptionEnabDisab)EditorGUILayout.EnumPopup("", RuntimePrefs.GenerateTrees);
                        }

                        if (RuntimePrefs.GenerateTrees == OptionEnabDisab.Enable)
                        {
                            using (new HorizontalBlock(GUI.skin.box))
                            {
                                GUILayout.Label(new GUIContent(" Trees Generation Mode  ", " Random to generate random trees/Vector to generate Trees from Vector file (OSM .. ) "), GUILayout.MaxWidth(200));
                                RuntimePrefs.TreesGenerationMode = (GenerationMode)EditorGUILayout.EnumPopup("", RuntimePrefs.TreesGenerationMode);
                            }
 
                            //Tree Prefabs List
                            using (new HorizontalBlock(GUI.skin.box))
                            {
                                GUILayout.Label("  Trees ", GUILayout.MaxWidth(200));
                                SerializedObject so = new SerializedObject(RuntimePrefs);
                                SerializedProperty stringsProperty = so.FindProperty("TreePrefabs");
                                EditorGUILayout.PropertyField(stringsProperty, true);
                                so.ApplyModifiedProperties();
                            }

                            using (new HorizontalBlock(GUI.skin.box))
                            {
                                GUILayout.Label(new GUIContent("                ", " "), GUILayout.MaxWidth(200));

                                if (GUILayout.Button(new GUIContent(" Load All ", "Click To Load all tree prefabs Located in 'Resources/Prefabs/Environment/Trees'"), GUILayout.ExpandWidth(true)))
                                {
                                    RuntimePrefs.LoadAllTreePrefabs();
                                }
                            }
                        }



                    }
                    using (new HorizontalBlock(GUI.skin.button))
                    {
                        GUILayout.Label(" Grass");
                    }
                    using (new VerticalBlock(GUI.skin.box))
                    {
                        using (new HorizontalBlock(GUI.skin.box))
                        {
                            GUILayout.Label(new GUIContent(" Generate Grass ", " Enable/Disable to generate terrain Grass "), GUILayout.MaxWidth(200));
                            RuntimePrefs.GenerateGrass = (OptionEnabDisab)EditorGUILayout.EnumPopup("", RuntimePrefs.GenerateGrass);
                        }
                        if (RuntimePrefs.GenerateGrass == OptionEnabDisab.Enable)
                        {
                            using (new HorizontalBlock(GUI.skin.box))
                            {
                                GUILayout.Label(new GUIContent(" Grass Generation Mode  ", " Random to generate random Grass/Vector to generate Grass from Vector file (OSM .. ) "), GUILayout.MaxWidth(200));
                                RuntimePrefs.GrassGenerationMode = (GenerationMode)EditorGUILayout.EnumPopup("", RuntimePrefs.GrassGenerationMode);
                            }

                            //Grass Prefabs List
                            using (new HorizontalBlock(GUI.skin.box))
                            {
                                GUILayout.Label("  Grass ", GUILayout.MaxWidth(200));
                                SerializedObject so = new SerializedObject(RuntimePrefs);
                                SerializedProperty stringsProperty = so.FindProperty("GrassPrefabs");
                                EditorGUILayout.PropertyField(stringsProperty, true);
                                so.ApplyModifiedProperties();
                            }

                            using (new HorizontalBlock(GUI.skin.box))
                            {
                                GUILayout.Label(new GUIContent("                ", " "), GUILayout.MaxWidth(200));

                                if (GUILayout.Button(new GUIContent(" Load All ", "Click To Load all Grass prefabs Located in 'Resources/Prefabs/Environment/Grass'"), GUILayout.ExpandWidth(true)))
                                {
                                    RuntimePrefs.LoadAllGrassPrefabs();
                                }
                            }
                        }



                    }
 
                    using (new HorizontalBlock(GUI.skin.button))
                    {
                        GUILayout.Label(" Roads");
                    }
                    using (new HorizontalBlock(GUI.skin.box))
                    {
                        GUILayout.Label(new GUIContent(" Generate Roads ", " Enable/Disable to generate roads (Only from Vector Tile) "), GUILayout.MaxWidth(200));
                        RuntimePrefs.GenerateRoads = (OptionEnabDisab)EditorGUILayout.EnumPopup("", RuntimePrefs.GenerateRoads);
                    }

                }
            }

        }
        private Texture2D LoadTexture(string m_iconeName)
        {
            var tex = new Texture2D(35, 35);

            string[] guids = AssetDatabase.FindAssets(m_iconeName + " t:texture");
            if (guids != null && guids.Length > 0)
            {
                string iconPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                tex = (Texture2D)AssetDatabase.LoadAssetAtPath(iconPath, typeof(Texture2D));
            }

            return tex;
        }
    }
}
