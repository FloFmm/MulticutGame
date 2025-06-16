using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;

public class ChallengeSelection : MonoBehaviour {
    public GameObject buttonPrefab; // Assign MenuButtonPrefab in inspector
    public Transform contentParent; // Assign Content of Scroll View
    public GameObject headlinePrefab;
    void Start()
    {
        int i = 0;
        foreach (Challenge challenge in GameData.ChallengeList.Challenges)
        {

            GameObject newButton = Instantiate(buttonPrefab, contentParent);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = challenge.Name;
            // Set HighScore and TimeLimit (in 2 columns below)
            int totalGraphs = GameData.GraphList.Graphs.Count;
            int startIndex = Mathf.FloorToInt(challenge.MinDifficulty * totalGraphs);
            int endIndex = Mathf.CeilToInt(challenge.MaxDifficulty * totalGraphs);
            int levelCount = endIndex - startIndex;

            int highScore = GameData.GetHighScoreForChallenge(challenge).HighScore;
            newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_HighScore")
                .GetComponent<TMPro.TextMeshProUGUI>().text = $"BEST\n{highScore} | {levelCount}";

            newButton.transform.Find("VerticalGroup/HorizontalGroup/Text_TimeLimit")
                .GetComponent<TMPro.TextMeshProUGUI>().text = $"TIME\n{challenge.TimeLimit}(+{challenge.TimePerLevel})s";

            // Set the button's background color
            Image buttonImage = newButton.GetComponent<Image>();
            if (buttonImage != null && i < GameData.ColorPalette.edgeColors.Count)
                buttonImage.color = GameData.ColorPalette.edgeColors[i];
            else
                buttonImage.color = GameData.ColorPalette.edgeColors[GameData.ColorPalette.edgeColors.Count - 1];
            i++;
            newButton.GetComponent<Button>().onClick.AddListener(() => OnChallengeSelected(challenge));

        }
    }

    void OnChallengeSelected(Challenge challenge)
    {
        int totalGraphs = GameData.GraphList.Graphs.Count;
        int startIndex = Mathf.FloorToInt(challenge.MinDifficulty * totalGraphs);
        int endIndex = Mathf.CeilToInt(challenge.MaxDifficulty * totalGraphs);
        List<Graph> challengeGraphList = new List<Graph>();
        foreach (Graph graph in GameData.GraphList.Graphs.GetRange(startIndex, endIndex - startIndex))
        {
            challengeGraphList.Add(graph.DeepCopy());
        }


        System.Random rng = new System.Random((int)DateTime.Now.Ticks);
        GameData.SelectedChallenge = challenge;
        List<Graph> shuffled = challengeGraphList.OrderBy(_ => rng.Next()).ToList();
        GameData.SelectedChallengeGraphList = shuffled.Take(challenge.LevelCount).ToList();
        GameData.SelectedChallengeGraphIndex = 0;
        if (GameData.SelectedChallengeGraphList.Count > 0)
            SceneManager.LoadScene("GameScene");
    }
}