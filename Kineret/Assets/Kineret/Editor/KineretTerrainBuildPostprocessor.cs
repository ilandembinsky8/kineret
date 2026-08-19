using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class KineretTerrainBuildPostprocessor
{
    private const string TerrainFolderName = "TSS_North_38x41_2048";
    private static readonly string[] RequiredFiles =
    {
        "TerrainData.dat",
        "TerrainData_Hr.hor"
    };

    private static readonly string[] RequiredDirectories =
    {
        "DEMData",
        "RasterData",
        "RasterData_Low256",
        "HorizonDEMData",
        "HorizonRasterData",
        "HorizonRasterData_UniformHigh"
    };

[PostProcessBuild(1000)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        string sourceRoot = ResolveSourceRoot();
        if (string.IsNullOrEmpty(sourceRoot))
        {
            throw new BuildFailedException("[KineretTerrainBuild] Terrain source folder was not found. Set KINERET_TERRAIN_ROOT to the folder that contains TerrainData.dat.");
        }

        string playerRoot = GetPlayerRoot(pathToBuiltProject);
        if (string.IsNullOrEmpty(playerRoot))
            throw new BuildFailedException("[KineretTerrainBuild] Could not determine build output folder from: " + pathToBuiltProject);

        string destinationRoot = Path.Combine(playerRoot, "KineretTerrain");
        Directory.CreateDirectory(destinationRoot);

        int copiedFiles = 0;
        int skippedFiles = 0;
        long copiedBytes = 0;

        foreach (string fileName in RequiredFiles)
        {
            string source = Path.Combine(sourceRoot, fileName);
            if (!File.Exists(source))
                throw new BuildFailedException("Required Kineret terrain file is missing: " + source);

            CopyFileIncremental(source, Path.Combine(destinationRoot, fileName), ref copiedFiles, ref skippedFiles, ref copiedBytes);
        }

        foreach (string directoryName in RequiredDirectories)
        {
            string sourceDirectory = Path.Combine(sourceRoot, directoryName);
            if (!Directory.Exists(sourceDirectory))
                throw new BuildFailedException("Required Kineret terrain directory is missing: " + sourceDirectory);

            CopyDirectoryIncremental(sourceDirectory, Path.Combine(destinationRoot, directoryName), ref copiedFiles, ref skippedFiles, ref copiedBytes);
        }

        string marker = Path.Combine(destinationRoot, "KineretTerrainBuildInfo.txt");
        File.WriteAllText(marker,
            "Source=" + sourceRoot + Environment.NewLine +
            "Built=" + DateTime.Now.ToString("O") + Environment.NewLine +
            "CopiedFiles=" + copiedFiles + Environment.NewLine +
            "SkippedFiles=" + skippedFiles + Environment.NewLine);

        Debug.Log(string.Format(
            "[KineretTerrainBuild] Terrain deployment complete. Destination: {0} | copied {1} files ({2:F1} MB), skipped {3} unchanged files.",
            destinationRoot,
            copiedFiles,
            copiedBytes / 1024.0 / 1024.0,
            skippedFiles));
    }

    private static string ResolveSourceRoot()
    {
        string environmentPath = Environment.GetEnvironmentVariable("KINERET_TERRAIN_ROOT");
        if (IsTerrainRoot(environmentPath))
            return Path.GetFullPath(environmentPath);

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        DirectoryInfo current = string.IsNullOrEmpty(projectRoot) ? null : new DirectoryInfo(projectRoot);

        for (int depth = 0; current != null && depth < 10; depth++, current = current.Parent)
        {
            string candidate = Path.Combine(current.FullName, "map", TerrainFolderName);
            if (IsTerrainRoot(candidate))
                return candidate;

            candidate = Path.Combine(current.FullName, TerrainFolderName);
            if (IsTerrainRoot(candidate))
                return candidate;
        }

        return null;
    }

    private static bool IsTerrainRoot(string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
               Directory.Exists(path) &&
               File.Exists(Path.Combine(path, "TerrainData.dat"));
    }

    private static string GetPlayerRoot(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return null;

        if (Directory.Exists(outputPath))
            return outputPath;

        return Path.GetDirectoryName(outputPath);
    }

    private static void CopyDirectoryIncremental(
        string sourceDirectory,
        string destinationDirectory,
        ref int copiedFiles,
        ref int skippedFiles,
        ref long copiedBytes)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
        {
            string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
            CopyFileIncremental(sourceFile, destinationFile, ref copiedFiles, ref skippedFiles, ref copiedBytes);
        }

        foreach (string sourceSubDirectory in Directory.GetDirectories(sourceDirectory))
        {
            string destinationSubDirectory = Path.Combine(destinationDirectory, Path.GetFileName(sourceSubDirectory));
            CopyDirectoryIncremental(sourceSubDirectory, destinationSubDirectory, ref copiedFiles, ref skippedFiles, ref copiedBytes);
        }
    }

    private static void CopyFileIncremental(
        string source,
        string destination,
        ref int copiedFiles,
        ref int skippedFiles,
        ref long copiedBytes)
    {
        FileInfo sourceInfo = new FileInfo(source);
        FileInfo destinationInfo = new FileInfo(destination);

        bool unchanged = destinationInfo.Exists &&
                         destinationInfo.Length == sourceInfo.Length &&
                         destinationInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc;

        if (unchanged)
        {
            skippedFiles++;
            return;
        }

        string destinationDirectory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        File.Copy(source, destination, true);
        File.SetLastWriteTimeUtc(destination, sourceInfo.LastWriteTimeUtc);
        copiedFiles++;
        copiedBytes += sourceInfo.Length;
    }
}
