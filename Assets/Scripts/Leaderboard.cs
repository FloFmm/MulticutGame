using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;
public class Leaderboard : MonoBehaviour
{
    public GameObject buttonPrefab; // Assign MenuButtonPrefab in inspector
    public Transform contentParent; // Assign Content of Scroll View
    // public GameObject textPrefab;
    // void Start()
    // {
    //     
    //     if (PlayGamesPlatform.Instance.IsAuthenticated())
    //         PlayGamesPlatform.Instance.ShowLeaderboardUI("CgkI2bj44u0bEAIQAQ");

    //     // Show leaderboard UI
    //     int i = 0;

    //     GameObject newButton = Instantiate(buttonPrefab, contentParent);
    //     // Set player name
    //     newButton.transform.Find("table/Player_Name")
    //         .GetComponent<TMPro.TextMeshProUGUI>().text = "PLAYER";

    //     // Find table transform
    //     Transform table = newButton.transform.Find("table");

    //     GameObject chScoreGO = Instantiate(textPrefab, table);
    //     var chScoreTMP = chScoreGO.GetComponent<TMPro.TextMeshProUGUI>();
    //     chScoreTMP.text = "LEVELS COMPLETED";

    //     RectTransform textRect = chScoreGO.GetComponent<RectTransform>();

    //     // Add each challenge
    //     foreach (Challenge challenge in GameData.ChallengeList.Challenges)
    //     {
    //         chScoreGO = Instantiate(textPrefab, table);
    //         chScoreTMP = chScoreGO.GetComponent<TMPro.TextMeshProUGUI>();
    //         chScoreTMP.text = $"{challenge.Name}";

    //         textRect = chScoreGO.GetComponent<RectTransform>();
    //     }

    //     // Make button transparent
    //     Image buttonImage = newButton.GetComponent<Image>();
    //     buttonImage.color = new Color(1f, 1f, 1f, 0f);

    //     foreach (Entry entry in GameData.Leaderboard.LeaderboardEntries)
    //     {

    //         newButton = Instantiate(buttonPrefab, contentParent);
    //         newButton.transform.Find("table/Player_Name")
    //             .GetComponent<TMPro.TextMeshProUGUI>().text = entry.PlayerName;

    //         table = newButton.transform.Find("table");
    //         // Count graphs where OptimalCost == BestAchievedCost
    //         int completedLevelsCount = entry.GraphHighScoreList.Graphs
    //             .Count(g => g.OptimalCost == g.BestAchievedCost);

    //         // Create first text: count of completed graphs (OptimalCost == BestAchievedCost)
    //         GameObject completedLevelCountObj = Instantiate(textPrefab, table);
    //         var completedLevelsText = completedLevelCountObj.GetComponent<TMPro.TextMeshProUGUI>();
    //         completedLevelsText.text = $"{completedLevelsCount}";

    //         // Instantiate TextMeshPro for each challenge highscore
    //         foreach (Challenge challenge in GameData.ChallengeList.Challenges)
    //         {
    //             // Try to find a matching high score for this challenge
    //             var chScore = entry.ChallengeHighScoreList.ChallengeHighScores
    //                 .FirstOrDefault(hs => hs.ChallengeName == challenge.Name
    //                                 && hs.ChallengeCreatedAt == challenge.CreatedAt);

    //             chScoreGO = Instantiate(textPrefab, table);
    //             chScoreTMP = chScoreGO.GetComponent<TMPro.TextMeshProUGUI>();

    //             if (chScore != null)
    //             {
    //                 chScoreTMP.text = $"{chScore.HighScore}";
    //             }
    //             else
    //             {
    //                 chScoreTMP.text = "";
    //             }
    //         }

    //         // Set the button's background color
    //         buttonImage = newButton.GetComponent<Image>();
    //         if (buttonImage != null && i < GameData.ColorPalette.edgeColors.Count)
    //             buttonImage.color = GameData.ColorPalette.edgeColors[i];
    //         else
    //             buttonImage.color = GameData.ColorPalette.edgeColors[GameData.ColorPalette.edgeColors.Count - 1];
    //         i++;
    //     }
    // }
    
    void SignInToGooglePlay()
    {
        if (!Social.localUser.authenticated)
        {
            Social.localUser.Authenticate((bool success) => {
                if (success)
                {
                    
                    Debug.Log("Signed in to Google Play Games Services");
                }
                else
                {
                    
                    Debug.Log("Failed to sign in to Google Play Games Services");
                }
            });
        }
        else
        {
            Debug.Log("Already signed in to Google Play Games");
        }
    }

    void Start()
    {
        SignInToGooglePlay();
        CreateButtons();
        UploadLeaderboardStats();
    }


    public void CreateButtons()
    {
        GameObject newButton = Instantiate(buttonPrefab, contentParent);
        newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "LEVELS";
        Image buttonImage = newButton.GetComponent<Image>();
        buttonImage.color = GameData.ColorPalette.edgeColors[0];
        newButton.GetComponent<Button>().onClick.AddListener(() => OnLeaderboardSelected("CgkI2bj44u0bEAIQAQ"));

        int totalGraphs = GameData.GraphHighScoreList.Graphs.Count;
        int completedGraphs = GameData.GraphHighScoreList.Graphs
            .Count(graph => graph.OptimalCost == graph.BestAchievedCost);
        newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_HighScore")
                .GetComponent<TMPro.TextMeshProUGUI>().text = $"COMPLETED\n{completedGraphs} | {totalGraphs}";

        newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_TimeLimit")
            .gameObject.SetActive(false);


        int i = 0;
        foreach (Challenge challenge in GameData.ChallengeList.Challenges)
        {

            newButton = Instantiate(buttonPrefab, contentParent);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"{challenge.Name}\nCHALLENGE";
            // Set HighScore and TimeLimit (in 2 columns below)
            int startIndex = Mathf.FloorToInt(challenge.MinDifficulty * totalGraphs);
            int endIndex = Mathf.CeilToInt(challenge.MaxDifficulty * totalGraphs);
            int levelCount = endIndex - startIndex;

            int highScore = GameData.GetHighScoreForChallenge(challenge).HighScore;
            newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_HighScore")
                .GetComponent<TMPro.TextMeshProUGUI>().text = $"MY HIGH SCORE\n{highScore} | {levelCount}";

            newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_TimeLimit")
                .gameObject.SetActive(false);
            // Set the button's background color
            buttonImage = newButton.GetComponent<Image>();
            if (buttonImage != null && i < GameData.ColorPalette.edgeColors.Count)
                buttonImage.color = GameData.ColorPalette.edgeColors[i];
            else
                buttonImage.color = GameData.ColorPalette.edgeColors[GameData.ColorPalette.edgeColors.Count - 1];
            i++;
            newButton.GetComponent<Button>().onClick.AddListener(() => OnLeaderboardSelected(challenge.LeaderBoardId));
        }
    }

    public void UploadLeaderboardStats()
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            int completedGraphs = GameData.GraphHighScoreList.Graphs
                .Count(graph => graph.OptimalCost == graph.BestAchievedCost);
            Social.ReportScore(completedGraphs, "CgkI2bj44u0bEAIQAQ", (bool success) =>
            {
                // Handle success or failure
            });


            foreach (Challenge challenge in GameData.ChallengeList.Challenges)
            {
                int highScore = GameData.GetHighScoreForChallenge(challenge).HighScore;
                Social.ReportScore(highScore, challenge.LeaderBoardId, (bool success) =>
                {
                    // Handle success or failure
                });
            }
        }
    }

    void OnLeaderboardSelected(string leaderbaordID)
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderbaordID);
    }
}