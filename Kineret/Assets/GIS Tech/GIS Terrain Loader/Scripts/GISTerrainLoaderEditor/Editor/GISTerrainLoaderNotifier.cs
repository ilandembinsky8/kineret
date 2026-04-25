
using UnityEditor;

public class GISTerrainLoaderNotifier
{
    private const string SessionKey = "GTL_Import_Notice_Shown";

    static GISTerrainLoaderNotifier()
    {
        // Delay call ensures AssetDatabase is ready
        EditorApplication.delayCall += ShowNoticeIfNeeded;
    }

    private static void ShowNoticeIfNeeded()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        EditorUtility.DisplayDialog(
            "GIS Terrain Loader Pro – Demo Data",
            "GIS Terrain Loader Pro has been imported successfully.\n\n" +
            "To use the demo scenes, please also import the package that contains GIS demo data.\n\n" +
            "Without it, demo scenes will load empty terrain. Link in Readme.txt file",
            "OK"
        );
    }
}
