using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Collections;


public class LevelSelection : MonoBehaviour
{
    public GameObject buttonPrefab; // Assign MenuButtonPrefab in inspector
    public Transform contentParent; // Assign Content of Scroll View
    public GameObject sectionPrefab;
    public GameObject headlinePrefab;
    public ScrollRect scrollRect;
    private List<Graph> graphs;
    void Start()
    {
        if (GameData.IsTutorial)
            graphs = GameData.TutorialList.Graphs;
        else
            graphs = GameData.GraphHighScoreList.Graphs;

        int graphIndex = 0;
        string[] difficultyLabels = { "EASY", "MEDIUM", "HARD", "EXPERT", "EXTREME" };
        int sectionIndex = 0;
        GameObject currentHeadline = null;
        GameObject currentSection = null;

        if (GameData.IsTutorial)
        {
            currentHeadline = Instantiate(headlinePrefab, contentParent);
            currentHeadline.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "TUTORIAL";
            currentSection = Instantiate(sectionPrefab, contentParent);
        }

        foreach (Graph graph in graphs)
        {
            if (!GameData.IsTutorial && graphIndex % (int)Math.Ceiling(graphs.Count / 5.0) == 0)
            {
                currentHeadline = Instantiate(headlinePrefab, contentParent);
                currentHeadline.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = difficultyLabels[sectionIndex];
                sectionIndex++;

                currentSection = Instantiate(sectionPrefab, contentParent);
            }
            GameObject newButton = Instantiate(buttonPrefab, currentSection.transform);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = graph.Name;

            // Compute ratio
            float ratio = (graph.OptimalCost != 0f) ? (graph.BestAchievedCost / (float)graph.OptimalCost) : 1f;
            ratio = Mathf.Clamp01(ratio); // Ensure between 0 and 1

            // Determine color bucket
            int colorIndex;
            if (ratio >= 1f)
                colorIndex = 0;
            else if (ratio >= 0.75f)
                colorIndex = 1;
            else if (ratio >= 0.5f)
                colorIndex = 2;
            else if (ratio >= 0.25f)
                colorIndex = 3;
            else
                colorIndex = 4;

            // Apply color (to Image component on the button)
            var image = newButton.GetComponent<Image>();
            if (image != null)
                image.color = GameData.ColorPalette.edgeColors[colorIndex];

            // Optional: add a click handler
            int currentIndex = graphIndex; // fixes closure issue
            newButton.GetComponent<Button>().onClick.AddListener(() => OnLevelSelected(currentIndex));
            graphIndex++;
        }

        StartCoroutine(RestoreScrollPositionNextFrame(GameData.levelSelectionScrollPosition));
    }

    IEnumerator RestoreScrollPositionNextFrame(float lastYPosition)
    {
        yield return null; // Wait 1 frame
        yield return null; // Wait 1 frame
        yield return null; // Wait 1 frame
        scrollRect.verticalNormalizedPosition = lastYPosition;
    }

    public void OnScrollChanged()
    {
        GameData.levelSelectionScrollPosition = scrollRect.verticalNormalizedPosition;
    }

    void OnLevelSelected(int graphIndex)
    {
        GameData.SelectedChallenge = null;
        GameData.SelectedGraphIndex = graphIndex;
        SceneManager.LoadScene("GameScene");
    }
}