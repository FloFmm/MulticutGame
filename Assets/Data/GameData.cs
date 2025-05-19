using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public static class GameData
{
    public static float levelSelectionScrollPosition = 0.0f;
    public static GraphList GraphList;
    public static ChallengeList ChallengeList;
    public static Graph SelectedGraph;
    public static Challenge SelectedChallenge;
    public static List<Graph> SelectedChallengeGraphList;
    public static int SelectedChallengeGraphIndex;
    public static bool ScissorIsActive; // toggle between scissor and rubber
    public static List<Vector3> LastCutPathPositions;
    public static List<GameObject> LastCutEdges;
    public static ColorPalette ColorPalette => _palette ??= Resources.Load<ColorPalette>("GlobalColorPalette");
    private static ColorPalette _palette;
    public static readonly List<int> edgeCosts = new List<int> { -2, -1, 0, 1, 2 };
    public static readonly List<float> edgeWidths = new List<float> { 6f, 6f, 6f, 6f, 6f };
    public static readonly List<float> edgePitches = new List<float> { 0.2f, 0.55f, 0.9f, 1.25f, 1.6f };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // This ensures _palette is loaded once early
        _ = ColorPalette;

        LoadGraphList();
        LoadChallengeList();
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
    
    public static void LoadChallengeList()
    {
        // Check if the GraphList exists in PlayerPrefs
        TextAsset jsonFile = Resources.Load<TextAsset>("challenges");
        if (jsonFile == null)
        {
            Debug.LogError("Challenge file not found in Resources.");
        }
        ChallengeList = JsonUtility.FromJson<ChallengeList>(jsonFile.text);
        if (ChallengeList == null)
        {
            throw new InvalidOperationException("No Challenges were loaded. Ensure the Challenge data exists");
        }
    }
}