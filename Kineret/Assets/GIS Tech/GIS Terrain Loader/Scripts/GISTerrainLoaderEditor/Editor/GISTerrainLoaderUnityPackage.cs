 
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
 
namespace GISTech.GISTerrainLoader
{
 

    public static class GISTerrainLoaderUnityPackage
    {
        public static void ExportUnityPackage(
            GISTerrainContainer container,
            string packagePath,
            ExportPackageOptions exportPackageOptions,
            OptionEnabDisab exportTextures,
            OptionEnabDisab exportMaterials,
            OptionEnabDisab exportShaders,
            OptionEnabDisab exportMesh,
            OptionEnabDisab exportScripts)
        {
            var root = "Assets/Exported GIS Terrains";

            if (!AssetDatabase.IsValidFolder(root))
                AssetDatabase.CreateFolder("Assets", "Exported GIS Terrains");

 

 

            ExportPackage(container, packagePath, root, exportPackageOptions, exportTextures, exportMaterials, exportShaders, exportMesh, exportScripts);

        }
 
        private static void ExportPackage(GISTerrainContainer container,
            string exportpackagePath, string root,
            ExportPackageOptions exportPackageOptions,
            OptionEnabDisab exportTextures,
            OptionEnabDisab exportMaterials,
            OptionEnabDisab exportShaders,
            OptionEnabDisab exportMeshes,
            OptionEnabDisab exportScripts)
        {
            // Validate selection
            var terrainGO = Selection.activeGameObject;
            if (terrainGO == null || terrainGO.GetComponent<GISTerrainContainer>() == null)
            {
                Debug.LogError("[ExportTerrain] Please select a GameObject with a Terrain component.");
                return;
            }



            string TerrainName = Path.GetFileNameWithoutExtension(container.SourceDEMPath);

            string MainTerrainFolder = root + "/"+TerrainName;

            if (!AssetDatabase.IsValidFolder(MainTerrainFolder))
                AssetDatabase.CreateFolder(root, TerrainName);


            var Dependencies_Folder = root + "/" + TerrainName+ "/Dependencies";



            // Prepare folders
            if (!AssetDatabase.IsValidFolder(Dependencies_Folder))
                AssetDatabase.CreateFolder(MainTerrainFolder, "Dependencies");

            AssetDatabase.Refresh();
            // Persist generated assets
            foreach (var mf in terrainGO.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh;
                if (mesh != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                {
                    string meshDir = Path.Combine(Dependencies_Folder, "Meshes");
                    if (!AssetDatabase.IsValidFolder(meshDir))
                        AssetDatabase.CreateFolder(Dependencies_Folder, "Meshes");
                    string path = $"{meshDir}/{mf.gameObject.name}_{mesh.GetInstanceID()}.mesh";
                    AssetDatabase.CreateAsset(Object.Instantiate(mesh), path);
                    mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                }
            }
            foreach (var mr in terrainGO.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)))
                    {
                        string matDir = Path.Combine(Dependencies_Folder, "Materials");
                        if (!AssetDatabase.IsValidFolder(matDir))
                            AssetDatabase.CreateFolder(Dependencies_Folder, "Materials");
                        string path = $"{matDir}/{mr.gameObject.name}_{mat.name}.mat";
                        AssetDatabase.CreateAsset(Object.Instantiate(mat), path);
                        mats[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                }
                mr.sharedMaterials = mats;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();


            // Create or overwrite prefab
            string prefabPath = Path.Combine(MainTerrainFolder, terrainGO.name + ".prefab");
            if (File.Exists(prefabPath))
            {
                // Overwrite existing prefab
                PrefabUtility.SaveAsPrefabAsset(terrainGO, prefabPath, out bool success);
            }
            else
            {
                // Create new prefab
                PrefabUtility.SaveAsPrefabAssetAndConnect(terrainGO, prefabPath, InteractionMode.UserAction);
                Debug.Log($"[ExportTerrain] Created new prefab: {prefabPath}");
            }
 

            // Gather all dependencies
            var allDeps = AssetDatabase.GetDependencies(prefabPath, true).ToList();

            var filtered = allDeps.Where(path =>
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                bool isMeta = ext == ".meta";
                if (isMeta)
                {
                    ext = Path.GetExtension(path.Substring(0, path.Length - ext.Length)).ToLowerInvariant();
                }
                bool keep = true;
                switch (ext)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".tga":
                    case ".psd":
                    case ".bmp":
                        keep = exportTextures == OptionEnabDisab.Enable;
                        break;
                    case ".mat":
                        keep = exportMaterials == OptionEnabDisab.Enable;
                        break;
                    case ".shader":
                    case ".shadergraph":
                        keep = exportShaders == OptionEnabDisab.Enable;
                        break;
                    case ".fbx":
                    case ".obj":
                    case ".mesh":
                        keep = exportMeshes == OptionEnabDisab.Enable;
                        break;
                    case ".cs":
                        keep = exportScripts == OptionEnabDisab.Enable;
                        break;
                }
                // Log decision for debugging
                return keep;
            }).ToArray();

            var removed = allDeps.Except(filtered).ToList();

            if (string.IsNullOrEmpty(exportpackagePath))
                return;

            AssetDatabase.ExportPackage(filtered, exportpackagePath, exportPackageOptions);
        }
 
    }

}
 