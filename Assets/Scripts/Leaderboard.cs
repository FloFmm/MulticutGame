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

    void Start()
    {
        CreateButtons();
    }

    public void CreateButtons()
    {
        GameObject newButton = Instantiate(buttonPrefab, contentParent);
        newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "LEVELS\nCOMPLETED";
        Image buttonImage = newButton.GetComponent<Image>();
        buttonImage.color = GameData.ColorPalette.edgeColors[0];
        newButton.GetComponent<Button>().onClick.AddListener(() => OnLeaderboardSelected("CgkI2bj44u0bEAIQAQ"));

        int totalGraphs = GameData.GraphHighScoreList.Graphs.Count;
        int completedGraphs = GameData.GraphHighScoreList.Graphs
            .Count(graph => graph.OptimalCost == graph.BestAchievedCost);
        // newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_HighScore")
        //         .GetComponent<TMPro.TextMeshProUGUI>().text = $"COMPLETED\n{completedGraphs} | {totalGraphs}";

        // newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_TimeLimit")
        //     .gameObject.SetActive(false);


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
            // newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_HighScore")
            //     .GetComponent<TMPro.TextMeshProUGUI>().text = $"MY HIGH SCORE\n{highScore} | {levelCount}";

            // newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_TimeLimit")
            //     .gameObject.SetActive(false);
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

    void OnLeaderboardSelected(string leaderbaordID)
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderbaordID);
    }
}