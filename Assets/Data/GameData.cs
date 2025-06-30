using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public static class GameData
{
    // Graph
    public static GraphList GraphList;
    public static GraphList GraphHighScoreList;
    public static Graph SelectedGraph;
    public static int SelectedGraphIndex;

    // Challenge
    public static ChallengeList ChallengeList;
    public static ChallengeHighScoreList ChallengeHighScoreList;
    public static Challenge SelectedChallenge;
    public static List<Graph> SelectedChallengeGraphList;
    public static int SelectedChallengeGraphIndex;
    public static float ChallengeStartTime;

    // Leaderboard
    // public static LeaderboardEntryList Leaderboard;

    // Tutorial
    public static bool IsTutorial = false;
    public static GraphList TutorialList;

    // UI
    public static float levelSelectionScrollPosition = 1.0f;
    public static bool ScissorIsActive; // toggle between scissor and rubber
    public static List<Vector3> LastCutPathPositions;
    public static List<GameObject> LastCutEdges;
    public static ColorPalette ColorPalette => _palette ??= Resources.Load<ColorPalette>("GlobalColorPalette");
    private static ColorPalette _palette;

    // Settings
    public static bool SoundIsOn = true;
    public static readonly List<int> edgeCosts = new List<int> { -2, -1, 0, 1, 2 };
    public static readonly List<float> edgeWidths = new List<float> { 6f, 6f, 6f, 6f, 6f };
    public static readonly List<float> edgePitches = new List<float> { 0.6f, 0.8f, 1.0f, 1.3f, 1.6f };
    public static readonly List<float> edgeVolumes = new List<float> { 1.0f, 0.85f, 0.7f, 0.55f, 0.4f };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // This ensures _palette is loaded once early
        _ = ColorPalette;

        LoadTutorialList();
        LoadGraphList();
        LoadGraphHighScoreList();
        LoadChallengeList();
        LoadChallengeHighScoreList();
        // LoadLeaderboard();
        LastCutPathPositions = new List<Vector3>();
        LastCutEdges = new List<GameObject>();
    }

    // public static void SaveToPlayerPrefs<T>(string key, T data)
    // {
    //     string json = JsonUtility.ToJson(data);
    //     PlayerPrefs.SetString(key, json);
    //     PlayerPrefs.Save();
    // }
    public static async Task SaveToPlayerPrefs<T>(string key, T data)
    {
        // Serialize JSON off main thread (can be slow on big data)
        string json = await Task.Run(() => JsonUtility.ToJson(data));

        // Set PlayerPrefs on main thread
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public static T LoadFromPlayerPrefs<T>(string key) where T : class
    {
        if (PlayerPrefs.HasKey(key))
        {
            string json = PlayerPrefs.GetString(key);
            return JsonUtility.FromJson<T>(json);
        }
        return null;
    }

    public static T LoadFromFile<T>(string key) where T : class, new()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(key);
        if (jsonFile == null)
        {
            Debug.LogError($"File not found at Resources/{key}");
            return new T();
        }
        return JsonUtility.FromJson<T>(jsonFile.text);
    }

    public static void LoadGraphList()
    {
        GraphList = LoadFromFile<GraphList>("graphList");
    }

    public static void LoadTutorialList()
    {
        TutorialList = LoadFromFile<GraphList>("tutorialList");
    }

    // public static void LoadLeaderboard()
    // {
    //     Leaderboard = LoadFromFile<LeaderboardEntryList>("leaderboard");
    // }

    public static void LoadGraphHighScoreList()
    {
        GraphHighScoreList = LoadFromPlayerPrefs<GraphList>("graphHighScoreList");

        if (GraphHighScoreList != null)
        {
            // Step 1: Keep only graphs that match by name and CreatedAt
            GraphHighScoreList.Graphs = GraphHighScoreList.Graphs
                .Where(g1 => GraphList.Graphs.Any(g2 =>
                    g1.Name == g2.Name && g1.CreatedAt == g2.CreatedAt))
                .ToList();

            // Step 2: Add any missing graphs from GraphList
            foreach (var graph in GraphList.Graphs)
            {
                bool exists = GraphHighScoreList.Graphs.Any(g =>
                    g.Name == graph.Name && g.CreatedAt == graph.CreatedAt);

                if (!exists)
                {
                    GraphHighScoreList.Graphs.Add(graph.DeepCopy()); // or deep copy
                }
            }
        }
        else
        {
            // If nothing loaded, use a full deep copy from GraphList
            GraphHighScoreList = GraphList.DeepCopy();
        }
    }

    public static void LoadChallengeList()
    {
        ChallengeList = LoadFromFile<ChallengeList>("challengeList");
    }

    public static void LoadChallengeHighScoreList()
    {
        ChallengeHighScoreList = LoadFromPlayerPrefs<ChallengeHighScoreList>("challengeHighScoreList");
        if (ChallengeHighScoreList != null)
        {
            // Filter out any high scores that don't match a challenge in the ChallengeList
            ChallengeHighScoreList.ChallengeHighScores = ChallengeHighScoreList.ChallengeHighScores
                .Where(score => ChallengeList.Challenges.Any(challenge =>
                    challenge.Name == score.ChallengeName &&
                    challenge.CreatedAt == score.ChallengeCreatedAt))
                .ToList();
        }
        else
            ChallengeHighScoreList = new ChallengeHighScoreList();
    }

    public static ChallengeHighScore GetHighScoreForChallenge(Challenge challenge)
    {
        var existingHighScore = ChallengeHighScoreList.ChallengeHighScores
            .FirstOrDefault(score =>
                score.ChallengeName == challenge.Name &&
                score.ChallengeCreatedAt == challenge.CreatedAt);
        if (existingHighScore != null)
        {
            return existingHighScore;
        }

        // Create new high score if none found
        var newHighScore = new ChallengeHighScore
        {
            ChallengeName = challenge.Name,
            ChallengeCreatedAt = challenge.CreatedAt,
        };

        // add the new high score to the list so it’s tracked
        ChallengeHighScoreList.ChallengeHighScores.Add(newHighScore);

        return newHighScore;
    }

    public static void LoadLevelOrChallenge()
    {
        if (SelectedChallenge == null) // level or tutorial
        {
            if (IsTutorial)
                SelectedGraph = TutorialList.Graphs[SelectedGraphIndex];
            else
                SelectedGraph = GraphHighScoreList.Graphs[SelectedGraphIndex];
        }
        else
        {
            SelectedGraph = SelectedChallengeGraphList[SelectedChallengeGraphIndex];
        }
    }
}