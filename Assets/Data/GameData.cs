using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public static class GameData
{
    public static GraphList GraphList;
    public static Graph SelectedGraph;
    public static bool ScissorIsActive; // toggle between scissor and rubber
    public static List<Vector3> LastCutPathPositions;
    public static List<GameObject> LastCutEdges;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        LoadGraphList();
        LastCutPathPositions = new List<Vector3>();
        LastCutEdges = new List<GameObject>();
    }

    public static void SaveGraphListToPlayerPrefs()
    {
        // Convert the GraphList to JSON
        string json = JsonUtility.ToJson(GraphList);

        // Store it in PlayerPrefs
        PlayerPrefs.SetString("graphList", json);
        PlayerPrefs.Save();
    }

    public static void LoadGraphList()
    {
        // Check if the GraphList exists in PlayerPrefs
        if (PlayerPrefs.HasKey("graphList"))
        {
            // Retrieve the JSON string from PlayerPrefs
            string json = PlayerPrefs.GetString("graphList");
            // Convert the JSON string back into the GraphList object
            GraphList = JsonUtility.FromJson<GraphList>(json);
            Debug.Log("Loaded Graph from PlayerPrefs.");
        }
        else
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("graphs");
            if (jsonFile == null)
            {
                Debug.LogError("Graph file not found in Resources.");
            }
            GraphList = JsonUtility.FromJson<GraphList>(jsonFile.text);
            Debug.Log("No player progress found.");
        }
        if (GraphList == null || GraphList.Graphs.Count == 0)
        {
            throw new InvalidOperationException("No graphs were loaded. Ensure the graph data exists and is not empty.");
        }
    }
}