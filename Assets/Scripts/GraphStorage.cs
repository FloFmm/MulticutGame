using System.IO;
using UnityEngine;

public static class GraphStorage
{
    // private static string FilePath => Path.Combine(Application.persistentDataPath, "graphs.json");
    private static string FilePath => Path.Combine(Application.streamingAssetsPath, "graphs.json");

    public static void SaveGraphs(GraphList graphList)
    {
        string json = JsonUtility.ToJson(graphList, true);
        File.WriteAllText(FilePath, json);
        Debug.Log($"Graphs saved to {FilePath}");
    }

    public static GraphList LoadGraphs()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogWarning("Graph file not found!");
            return new GraphList();
        }

        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<GraphList>(json);
    }
}
