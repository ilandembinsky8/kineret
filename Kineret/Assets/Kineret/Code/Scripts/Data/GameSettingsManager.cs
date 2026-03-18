using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class GameSettingsManager
{
    private static Dictionary<string, Dictionary<string, string>> iniData;

    static GameSettingsManager()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "config.ini");
        iniData = ParseIni(path);
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIni(string path)
    {
        var data = new Dictionary<string, Dictionary<string, string>>();
        if (!File.Exists(path)) return data;

        string currentSection = "";
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line[1..^1];
                if (!data.ContainsKey(currentSection))
                    data[currentSection] = new Dictionary<string, string>();
            }
            else if (line.Contains('='))
            {
                var parts = line.Split('=', 2);
                data[currentSection][parts[0].Trim()] = parts[1].Trim();
            }
        }

        return data;
    }

    public static float GetFloat(string section, string key, float fallback = 0f)
        => float.TryParse(Get(section, key), out var v) ? v : fallback;

    public static int GetInt(string section, string key, int fallback = 0)
        => int.TryParse(Get(section, key), out var v) ? v : fallback;

    public static string Get(string section, string key, string fallback = "")
    {
        if (iniData.TryGetValue(section, out var sec))
            if (sec.TryGetValue(key, out var value))
                return value;
        return fallback;
    }

}