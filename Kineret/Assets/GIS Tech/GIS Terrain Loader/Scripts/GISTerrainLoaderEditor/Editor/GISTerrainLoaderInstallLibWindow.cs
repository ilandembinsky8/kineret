using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderInstallLibWindow : EditorWindow
    {
        private const string LibsPath = "GIS Tech/GIS Terrain Loader/Resources/Libs";
        private const string IconesPath = "GIS Tech/GIS Terrain Loader/Resources/Icones/Libs";

        private int selectedTab = 0;
        private readonly string[] tabNames = { "Important Lib", "Readers", "Generators" };
        private Vector2 scrollPosition;

        private static Texture2D placeholderTexture;

        [MenuItem("Tools/GIS Tech/GIS Terrain Loader/Lib Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<GISTerrainLoaderInstallLibWindow>("GIS Terrain Loader - Install Libs");
            window.minSize = new Vector2(900, 650);
            window.maxSize = new Vector2(1200, 900);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Draw header
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            // Draw tabs
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            
            EditorGUILayout.Space(10);
            
            // Draw content based on selected tab
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0:
                    DrawImportantLibTab();
                    break;
                case 1:
                    DrawReadersTab();
                    break;
                case 2:
                    DrawGeneratorsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("GIS Terrain Loader - Library Manager", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Manage your GIS libraries and add-ons", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawImportantLibTab()
        {
            DrawAssetCard(AssetDefinitions.UpgradeToUnity6);
            DrawAssetCard(AssetDefinitions.DotSpatial);
            DrawAssetCard(AssetDefinitions.FileBrowser);
            DrawAssetCard(AssetDefinitions.Pdal);
            DrawAssetCard(AssetDefinitions.GeoJson);
        }

        private void DrawReadersTab()
        {
            DrawAssetCard(AssetDefinitions.ENC57Reader);
            DrawAssetCard(AssetDefinitions.RasterDrivenSplatmaps);
        }

        private void DrawGeneratorsTab()
        {
            DrawAssetCard(AssetDefinitions.FenceGenerator);
            //DrawAssetCard(AssetDefinitions.AdvancedVectorGenerator);
            //DrawAssetCard(AssetDefinitions.GISTerrainStreamer);
        }

        private void DrawAssetCard(AssetInfo asset)
        {
            bool isInstalled = IsAssetInstalled(asset);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Thumbnail and info row
            EditorGUILayout.BeginHorizontal();
            
            // Thumbnail
            Texture2D thumbnail = LoadThumbnail(asset.ThumbnailPath);
            if (thumbnail != null)
            {
                GUILayout.Box(thumbnail, GUILayout.Width(120), GUILayout.Height(120));
            }
            else
            {
                GUILayout.Box("No Image", GUILayout.Width(120), GUILayout.Height(120));
            }
            
            EditorGUILayout.Space(10);
            
            // Asset info
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(asset.Name, EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Status indicator
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Status: ", EditorStyles.miniLabel);
            GUI.color = isInstalled ? Color.green : Color.red;
            GUILayout.Label(isInstalled ? "Installed" : "Not Installed", EditorStyles.miniBoldLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Install/Remove button
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            if (GUILayout.Button(isInstalled ? "Remove" : "Install", GUILayout.Height(30)))
            {
                if (isInstalled)
                {
                    RemoveAsset(asset);
                }
                else
                {
                    InstallAsset(asset);
                }
            }
 

            EditorGUILayout.HelpBox(asset.Description, MessageType.Info);

            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Links row
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            // Documentation link
            if (!string.IsNullOrEmpty(asset.DocumentationURL))
            {
                GUI.color = new Color(0.3f, 0.6f, 1f);
                if (GUILayout.Button("Documentation", GUILayout.Height(25)))
                {
                    Application.OpenURL(asset.DocumentationURL);
                }
                GUI.color = Color.white;
            }
            
            GUILayout.FlexibleSpace();


            // Asset Store link
            if (!string.IsNullOrEmpty(asset.StoreURL))
            {
                GUI.color = new Color(1f, 0.6f, 0.2f);
                if (GUILayout.Button("Link", GUILayout.Height(25)))
                {
                    Application.OpenURL(asset.StoreURL);
                }
                GUI.color = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
        }

        private Texture2D LoadThumbnail(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            
            string fullPath = Path.Combine(Application.dataPath, path);
            if (!File.Exists(fullPath))
                return null;
            
            return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/" + path);
        }

        private bool IsAssetInstalled(AssetInfo asset)
        {
            if (string.IsNullOrEmpty(asset.DefineSymbol))
                return false;
            
            var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone).Split(';').Select(d => d.Trim()).ToList();
            
            return defineSymbols.Contains(asset.DefineSymbol);
        }

        public static void AddSymbolToAllTargets(string defineSymbol)
        {
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (!IsValidBuildTargetGroup(group)) continue;

                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').Select(d => d.Trim()).ToList();
                if (!defineSymbols.Contains(defineSymbol))
                {
                    defineSymbols.Add(defineSymbol);
                    try
                    {
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defineSymbols.ToArray()));
                    }
                    catch (Exception)
                    {
                        Debug.Log("Could not set defines for build target group: " + group);
                        throw;
                    }
                }
            }
        }
        public static bool IsValidBuildTargetGroup(BuildTargetGroup group)
        {
            if (group == BuildTargetGroup.Unknown || IsObsolete(group)) return false;
#if UNITY_5_3_0 || UNITY_5_3 
            if ((int)(object)group == 25) return false;
#endif

#if UNITY_5_4 || UNITY_5_5 
            if ((int)(object)group == 15) return false;
            if ((int)(object)group == 16) return false;
#endif
            if (Application.unityVersion.StartsWith("5.6"))
            {
                if ((int)(object)group == 27) return false;
            }

            return true;
        }
        private static bool IsObsolete(Enum value)
        {
            var enumInt = (int)(object)value;
            if (enumInt == 4 || enumInt == 14) return false;

            var field = value.GetType().GetField(value.ToString());
            var attributes = (ObsoleteAttribute[])field.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            return attributes.Length > 0;
        }
        public static void RemoveSymbolFromAllTargets(string defineSymbol)
        {
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (!IsValidBuildTargetGroup(group)) continue;

                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').Select(d => d.Trim()).ToList();
                if (defineSymbols.Contains(defineSymbol))
                {
                    defineSymbols.Remove(defineSymbol);
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defineSymbols.ToArray()));
                }
            }
        }

        private void InstallAsset(AssetInfo asset)
        {
            string packagePath = Path.Combine(Application.dataPath, asset.PackagePath);
            
            if (!File.Exists(packagePath))
            {
                EditorUtility.DisplayDialog(
                    "Package Not Found",
                    $"The package file '{asset.PackagePath}' was not found.\n\nPlease ensure the package exists in the Resources/Libs folder.",
                    "OK"
                );
                return;
            }
            
            bool confirm = EditorUtility.DisplayDialog(
                "Install " + asset.Name,
                $"Are you sure you want to install {asset.Name}?",
                "Install",
                "Cancel"
            );
            
            if (!confirm)
                return;
            
            try
            {
                AssetDatabase.ImportPackage(packagePath, false);
                
                if (!string.IsNullOrEmpty(asset.DefineSymbol))
                {
                    AddSymbolToAllTargets(asset.DefineSymbol);
                }
                
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog(
                    "Installation Complete",
                    $"{asset.Name} has been successfully installed.",
                    "OK"
                );
                
                Debug.Log($"[GIS Terrain Loader] {asset.Name} installed successfully.");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Installation Failed",
                    $"Failed to install {asset.Name}:\n{ex.Message}",
                    "OK"
                );
                Debug.LogError($"[GIS Terrain Loader] Failed to install {asset.Name}: {ex.Message}");
            }
        }

        private void RemoveAsset(AssetInfo asset)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Remove " + asset.Name,
                $"Are you sure you want to remove {asset.Name}?\n\nThis action cannot be undone.",
                "Remove",
                "Cancel"
            );
            
            if (!confirm)
                return;
            
            try
            {
                // Remove all related files and folders
                foreach (string path in asset.RemovalPaths)
                {
                    string fullPath = Path.Combine(Application.dataPath, path);
                    
                    if (Directory.Exists(fullPath))
                    {
                        FileUtil.DeleteFileOrDirectory(fullPath);
                        FileUtil.DeleteFileOrDirectory(fullPath + ".meta");
                    }
                    else if (File.Exists(fullPath))
                    {
                        FileUtil.DeleteFileOrDirectory(fullPath);
                        FileUtil.DeleteFileOrDirectory(fullPath + ".meta");
                    }
                }
                
                // Remove scripting define symbol
                if (!string.IsNullOrEmpty(asset.DefineSymbol))
                {
                    RemoveSymbolFromAllTargets(asset.DefineSymbol);
                }
                
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog(
                    "Removal Complete",
                    $"{asset.Name} has been successfully removed.",
                    "OK"
                );
                
                Debug.Log($"[GIS Terrain Loader] {asset.Name} removed successfully.");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Removal Failed",
                    $"Failed to remove {asset.Name}:\n{ex.Message}",
                    "OK"
                );
                Debug.LogError($"[GIS Terrain Loader] Failed to remove {asset.Name}: {ex.Message}");
            }
        }

        #region Asset Definitions

        public static class AssetDefinitions
        {
            public static readonly AssetInfo UpgradeToUnity6 = new AssetInfo
            {
                Name = "Upgrade Materials To Unity 6 (URP)",
                ThumbnailPath = IconesPath + "/Unity6.jpg",
                PackagePath = LibsPath + "/Materials_URP.unitypackage",
                DefineSymbol = "",
                DocumentationURL = "",
                StoreURL = "",
                Description = "Upgrade Project Materials To Unity 6 (URP)",
                RemovalPaths = new string[]
    {
    }
            };
            public static readonly AssetInfo DotSpatial = new AssetInfo
            {
                Name = "DotSpatial",
                ThumbnailPath = IconesPath + "/DotSpatial.png",
                PackagePath = LibsPath + "/DotNetSpatial.unitypackage",
                DefineSymbol = "DotSpatial",
                DocumentationURL = "",
                StoreURL = "",
                Description = "DotSpatial is a geographic information system library written for .NET Framework (V1-V3) and .NET Core (V4+). " +
                "It allows GIS Terrain Loader Pro to read different Data Projections",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Plugins/DotSpatial"
                }
            };

            public static readonly AssetInfo FileBrowser = new AssetInfo
            {
                Name = "FileBrowser",
                ThumbnailPath = IconesPath + "/FileBrowser.png",
                PackagePath = LibsPath + "/FileBrowser.unitypackage",
                DefineSymbol = "FileBrowser",
                DocumentationURL = "https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006",
                StoreURL = "https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006",
                Description = "This plugin helps you show save/load dialogs during gameplay with its uGUI based file browser, used in the main Demo scene for Runtime GIS Terrain Loader Pro",
                RemovalPaths = new string[]
                {
                    "SimpleFileBrowser"
                }
            };

            public static readonly AssetInfo Pdal = new AssetInfo
            {
                Name = "Pdal",
                ThumbnailPath = IconesPath + "/Pdal.png",
                PackagePath = LibsPath + "/PdalLib.unitypackage",
                DefineSymbol = "GISTerrainLoaderPdal",
                DocumentationURL = "",
                StoreURL = "",
                Description = "This Lib helps GIS Terrain Loader Pro to generate terrains by loading Lidar data 'LAZ'",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Plugins/Lidar"
                }
            };

            public static readonly AssetInfo GeoJson = new AssetInfo
            {
                Name = "GeoJson",
                ThumbnailPath = IconesPath + "/GeoJson.png",
                PackagePath = LibsPath + "/GeoJson.unitypackage",
                DefineSymbol = "GISTerrainLoaderGeoJson",
                DocumentationURL = "https://github.com/GeoJSON-Net/GeoJSON.Net/blob/master/README.md",
                StoreURL = "https://github.com/GeoJSON-Net/GeoJSON.Net/blob/master/README.md",
                Description = "This Lib helps GIS Terrain Loader Pro to Read GeoJson Vector Data",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Plugins/GeoJSON.Net"
                }
            };

            public static readonly AssetInfo ENC57Reader = new AssetInfo
            {
                Name = "ENC57 Reader",
                ThumbnailPath = IconesPath + "/ENC57Reader.png",
                PackagePath = LibsPath + "/Readers/ENC57Reader/ENC57Reader_Add-on.unitypackage",
                DefineSymbol = "GISTechS57",
                DocumentationURL = "https://docs.example.com/enc57",
                StoreURL = "https://assetstore-fallback.unity.com/packages/tools/integration/enc57-electronic-navigational-charts-add-on-for-gis-terrain-load-322094",
                Description = "This Lib add-on helps GIS Terrain Loader Pro to read Real-World Nautical Data Vector Data",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Documentation/ENC57 Reader Add-On v1.0.pdf",
                    "GIS Tech/GIS Terrain Loader/Plugins/GISTech.S57",
                    "GIS Tech/GIS Terrain Loader/Resources/Prefabs/Environment/Buildings/ENC57_Buildings",
                    "GIS Tech/GIS Terrain Loader/Resources/Prefabs/Environment/GeoPoints/ENC57_Points",
                    "GIS Tech/GIS Terrain Loader/Resources/Prefabs/Environment/Roads/ENC57_Roads",
                    "GIS Tech/GIS Terrain Loader/Scenes/ENC57 Demo",
                    "GIS Tech/GIS Terrain Loader/Resources/GIS Terrains/Example_ENC57",
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderRuntime/GISTerrainLoaderAdd-on/GISTerrainLoaderS57Reader",
                    "StreamingAssets/Example_ENC57",
                    "GIS Tech/GIS Terrain Loader/Resources/Settings/GISTech.S57"
                }
            };

            public static readonly AssetInfo RasterDrivenSplatmaps = new AssetInfo
            {
                Name = "Raster Driven Splatmaps",
                ThumbnailPath = IconesPath + "/RasterDrivenSplatmaps.png",
                PackagePath = LibsPath + "/RasterDrivenSplatmaps_Add-on.unitypackage",
                DefineSymbol = "RDST",
                DocumentationURL = "https://www.gistech.org/rasterdrivensplatmaps-add-on",
                StoreURL = "https://assetstore.unity.com/packages/slug/353708",
                Description = "The RasterDrivenSplatmaps add-on enables advanced terrain texturing in Unity by using external raster masks (such as LiDAR classifications or segmented imagery) to control Unity Terrain splatmaps. This allows for ground-level terrain texturing based on real-world data.\r\n",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderEditor/Editor/GISTerrainLoaderAdd-on/RasterDrivenSplatmaps",
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderRuntime/GISTerrainLoaderAdd-on/RasterDrivenSplatmaps",
                    "GIS Tech/GIS Terrain Loader/Scenes/Raster Driven Splatmaps Demo",
                    "GIS Tech/GIS Terrain Loader/Resources/GIS Terrains/RasterDrivenSplatmaps",
                    "GIS Tech/GIS Terrain Loader/Documentation/RDST Raster-Driven Splatmaps.pdf"
                }
            };

            public static readonly AssetInfo FenceGenerator = new AssetInfo
            {
                Name = "Fence Generator",
                ThumbnailPath = IconesPath + "/FenceGenerator.png",
                PackagePath = LibsPath + "/FenceGenerator_Add-on.unitypackage",
                DefineSymbol = "GISTerrainLoaderFenceGenerator",
                DocumentationURL = "https://assetstore-fallback.unity.com/packages/tools/integration/fence-generator-add-on-for-gis-terrain-loader-pro-326705",
                StoreURL = "https://assetstore-fallback.unity.com/packages/tools/integration/fence-generator-add-on-for-gis-terrain-loader-pro-326705",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Documentation/Fence Generator Add-On v1.0.pdf",
                    "GIS Tech/GIS Terrain Loader/Resources/Prefabs/Environment/Add-ons/Fences",
                    "GIS Tech/GIS Terrain Loader/Scenes/Fence Generator",
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderEditor/Editor/GISTerrainLoaderAdd-on/GISTerrainLoaderFenceGenerator",
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderRuntime/GISTerrainLoaderAdd-on/GISTerrainLoaderFenceGenerator"
                }
            };

            public static readonly AssetInfo AdvancedVectorGenerator = new AssetInfo
            {
                Name = "Advanced Vector Generator",
                ThumbnailPath = IconesPath + "/AdvancedVectorGenerator.png",
                PackagePath = LibsPath + "/AdvancedVectorGenerator_Add-on.unitypackage",
                DefineSymbol = "GISTechAdvancedVectorGenerator",
                DocumentationURL = "https://docs.example.com/avg",
                StoreURL = "https://assetstore.unity.com/packages/avg",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Documentation/Advanced Vector Generator Add-On v1.0.pdf",
                    "GIS Tech/GIS Terrain Loader/Scenes/Advanced Vector Generator",
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderEditor/Editor/GISTerrainLoaderAdd-on/GISTerrainLoaderFenceGenerator",
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderRuntime/GISTerrainLoaderAdd-on/GISTerrainLoaderAVG",
                    "GIS Tech/GIS Terrain Loader/Resources/Settings/AVG_VectorMapStyle"
                }
            };

            public static readonly AssetInfo GISTerrainStreamer = new AssetInfo
            {
                Name = "GIS Terrain Streamer",
                ThumbnailPath = IconesPath + "/GISTerrainStreamer.png",
                PackagePath = LibsPath + "/GISTerrainStreamer_Add-on.unitypackage",
                DefineSymbol = "GISTerrainStreamer",
                DocumentationURL = "https://docs.example.com/streamer",
                StoreURL = "https://assetstore.unity.com/packages/streamer",
                RemovalPaths = new string[]
                {
                    "GIS Tech/GIS Terrain Loader/Documentation/GIS Terrain Streamer Add-On v1.0.pdf",
                    "GIS Tech/GIS Terrain Loader/Scenes/GIS Terrain Streamer",
                    "GIS Tech/GIS Terrain Loader/Plugins/Gdal",
                    "GIS Tech/GIS Terrain Loader/Scripts/GISTerrainLoaderRuntime/GISTerrainLoaderAdd-on/GISTerrainStreamer",
                    "StreamingAssets/proj"
                }
            };
        }

        #endregion
    }

    public class AssetInfo
    {
        public string Name { get; set; }
        public string ThumbnailPath { get; set; }
        public string PackagePath { get; set; }
        public string DefineSymbol { get; set; }
        public string DocumentationURL { get; set; }
        public string Description { get; set; }        
        public string StoreURL { get; set; }
        public string[] RemovalPaths { get; set; }
    }
    public class GISTerrainLoaderProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            string[] folders = Directory.GetFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories);
            string defineSymbol = "GISDataDownloader";

            foreach (string assetPath in deletedAssets)
            {
                if (assetPath.EndsWith("GISDataDownloaderEditor.asmdef"))
                {
                    GISTerrainLoaderInstallLibWindow.RemoveSymbolFromAllTargets(defineSymbol);

                    break;
                }
            }
        }
    }
}
