using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;

public class ChallengeSelection : MonoBehaviour {
    public GameObject buttonPrefab; // Assign MenuButtonPrefab in inspector
    public Transform contentParent; // Assign Content of Scroll View
    void Start()
    {
        int i = 0;
        foreach (Challenge challenge in GameData.ChallengeList.Challenges)
        {
            
            GameObject newButton = Instantiate(buttonPrefab, contentParent);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = challenge.Name;
            // Set HighScore and TimeLimit (in 2 columns below)
            newButton.transform.Find("HorizontalGroup/Text_HighScore")
                .GetComponent<TMPro.TextMeshProUGUI>().text = $"HIGH SCORE:\n{challenge.HighScore} | {challenge.LevelCount}";

            newButton.transform.Find("HorizontalGroup/Text_TimeLimit")
                .GetComponent<TMPro.TextMeshProUGUI>().text = $"TIME:\n{challenge.TimeLimit}s";

            // Set the button's background color
            Image buttonImage = newButton.GetComponent<Image>();
            if (buttonImage != null && i < GameData.ColorPalette.edgeColors.Count)
                buttonImage.color = GameData.ColorPalette.edgeColors[i];
            else
                buttonImage.color = GameData.ColorPalette.edgeColors[GameData.ColorPalette.edgeColors.Count-1];
            i++;
            newButton.GetComponent<Button>().onClick.AddListener(() => OnChallengeSelected(challenge));
        }
    }

    void OnChallengeSelected(Challenge challenge)
    {
        List<Graph> challengeGraphList = new List<Graph>();
        foreach (Graph graph in GameData.GraphList.Graphs)
        {
            if (graph.Difficulty >= challenge.MinDifficulty && graph.Difficulty <= challenge.MaxDifficulty)
            {
                Graph copiedGraph = new Graph
                {
                    Difficulty = graph.Difficulty,
                    Name = graph.Name,
                    OptimalCost = graph.OptimalCost,
                    BestAchievedCost = 0,
                    Nodes = graph.Nodes.Select(n => n.Clone()).ToList(),
                    Edges = graph.Edges.Select(e => e.Clone()).ToList()
                };
                //TODO resetting should not be necessary
                foreach (Edge edge in copiedGraph.Edges)
                {
                    edge.IsCut = false;
                }
                foreach (Node node in copiedGraph.Nodes)
                {
                    node.ConnectedComponentId = 0;
                }

                challengeGraphList.Add(copiedGraph);
            }
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