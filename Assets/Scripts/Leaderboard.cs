using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;

public class Leaderboard : MonoBehaviour {
    public GameObject buttonPrefab; // Assign MenuButtonPrefab in inspector
    public Transform contentParent; // Assign Content of Scroll View
    public GameObject textPrefab;
    void Start()
    {
        int i = 0;
        
        GameObject newButton = Instantiate(buttonPrefab, contentParent);
        // Set player name
        newButton.transform.Find("table/Player_Name")
            .GetComponent<TMPro.TextMeshProUGUI>().text = "PLAYER";

        // Find table transform
        Transform table = newButton.transform.Find("table");

        GameObject chScoreGO = Instantiate(textPrefab, table);
        var chScoreTMP = chScoreGO.GetComponent<TMPro.TextMeshProUGUI>();
        chScoreTMP.text = "LEVELS COMPLETED";

        RectTransform textRect = chScoreGO.GetComponent<RectTransform>();

        // Add each challenge
        foreach (Challenge challenge in GameData.ChallengeList.Challenges)
        {
            chScoreGO = Instantiate(textPrefab, table);
            chScoreTMP = chScoreGO.GetComponent<TMPro.TextMeshProUGUI>();
            chScoreTMP.text = $"{challenge.Name}";

            textRect = chScoreGO.GetComponent<RectTransform>();
        }

        // Make button transparent
        Image buttonImage = newButton.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0f);

        foreach (Entry entry in GameData.Leaderboard.LeaderboardEntries)
        {

            newButton = Instantiate(buttonPrefab, contentParent);
            newButton.transform.Find("table/Player_Name")
                .GetComponent<TMPro.TextMeshProUGUI>().text = entry.PlayerName;

            table = newButton.transform.Find("table");
            // Count graphs where OptimalCost == BestAchievedCost
            int completedLevelsCount = entry.GraphHighScoreList.Graphs
                .Count(g => g.OptimalCost == g.BestAchievedCost);

            // Create first text: count of completed graphs (OptimalCost == BestAchievedCost)
            GameObject completedLevelCountObj = Instantiate(textPrefab, table);
            var completedLevelsText = completedLevelCountObj.GetComponent<TMPro.TextMeshProUGUI>();
            completedLevelsText.text = $"{completedLevelsCount}";

            // Instantiate TextMeshPro for each challenge highscore
            foreach (Challenge challenge in GameData.ChallengeList.Challenges)
            {
                // Try to find a matching high score for this challenge
                var chScore = entry.ChallengeHighScoreList.ChallengeHighScores
                    .FirstOrDefault(hs => hs.ChallengeName == challenge.Name
                                    && hs.ChallengeCreatedAt == challenge.CreatedAt);

                chScoreGO = Instantiate(textPrefab, table);
                chScoreTMP = chScoreGO.GetComponent<TMPro.TextMeshProUGUI>();

                if (chScore != null)
                {
                    chScoreTMP.text = $"{chScore.HighScore}";
                }
                else
                {
                    chScoreTMP.text = "";
                }
            }

            // Set the button's background color
            buttonImage = newButton.GetComponent<Image>();
            if (buttonImage != null && i < GameData.ColorPalette.edgeColors.Count)
                buttonImage.color = GameData.ColorPalette.edgeColors[i];
            else
                buttonImage.color = GameData.ColorPalette.edgeColors[GameData.ColorPalette.edgeColors.Count - 1];
            i++;
        }
    }
}