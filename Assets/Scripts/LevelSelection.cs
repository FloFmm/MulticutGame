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
    public GameObject section;
    public GameObject nextButton;
    public GameObject headline;
    public ScrollRect scrollRect;
    private List<Graph> graphs;
    private List<GameObject> buttons = new List<GameObject>();
    private static string[] difficultyLabels = { "EASY", "MEDIUM", "HARD", "EXPERT", "EXTREME" };
    void Start()
    {
        if (GameData.IsTutorial)
            graphs = GameData.TutorialList.Graphs;
        else
            graphs = GameData.GraphHighScoreList.Graphs;

        int graphIndex = 0;
        int startIndex, endIndex;
        if (GameData.IsTutorial)
        {
            nextButton.SetActive(false);
            startIndex = 0;
            endIndex = graphs.Count;
            headline.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "TUTORIAL";
        }
        else
        {
            nextButton.SetActive(true);
            headline.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = difficultyLabels[GameData.LevelSelectionSectionIndex];
            int graphsPerSection = (int)Math.Ceiling(graphs.Count / (float)GameData.DifficultyCount);
            startIndex = GameData.LevelSelectionSectionIndex * graphsPerSection;
            endIndex = Mathf.Min(graphs.Count, (GameData.LevelSelectionSectionIndex + 1) * graphsPerSection);
            nextButton.gameObject.SetActive(true);
            nextButton.GetComponent<NextButton>().currentIndex = GameData.LevelSelectionSectionIndex;
            nextButton.GetComponent<NextButton>().slideCount = GameData.DifficultyCount;
            nextButton.GetComponent<NextButton>().UpdateText();
            var buttonComponent = nextButton.GetComponent<Button>();
            var scriptComponent = nextButton.GetComponent<NextButton>();
            buttonComponent.onClick.AddListener(() =>
            {
                scriptComponent.OnClicked(); // call its own OnClicked method
                // Update the global index
                GameData.LevelSelectionSectionIndex = scriptComponent.currentIndex;
                headline.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = difficultyLabels[GameData.LevelSelectionSectionIndex];
                // Call your refresh function with the new startIndex
                UpdateButtons();
            });
        }


        for (int i = startIndex; i < endIndex; i++)
        {
            Graph graph = graphs[i];
            GameObject newButton = Instantiate(buttonPrefab, section.transform);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = graph.Name;

            // Compute ratio
            float ratio = 0;
            if (!GameData.IsTutorial)
                ratio = (graph.OptimalCost != 0f) ? (graph.BestAchievedCost / (float)graph.OptimalCost) : 1f;

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
            buttons.Add(newButton);    
            graphIndex++;
        }
    }

    void OnLevelSelected(int graphIndex)
    {
        GameData.SelectedChallenge = null;
        GameData.SelectedGraphIndex = graphIndex;
        SceneManager.LoadScene("GameScene");
    }

    public void UpdateButtons()
    {
        int graphsPerSection = (int)Math.Ceiling(graphs.Count / (float)GameData.DifficultyCount);
        int startIndex = GameData.LevelSelectionSectionIndex * graphsPerSection;
        int pageSize = buttons.Count;

        for (int i = 0; i < pageSize; i++)
        {
            int graphIndex = startIndex + i;

            GameObject buttonGO = buttons[i];

            // Disable button if no corresponding graph
            if (graphIndex >= graphs.Count)
            {
                buttonGO.SetActive(false);
                continue;
            }
            else
            {
                buttonGO.SetActive(true);
            }

            Graph graph = graphs[graphIndex];

            // Update button text
            var tmp = buttonGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = graph.Name;

            // Compute ratio and determine color
            float ratio = (graph.OptimalCost != 0f) ? (graph.BestAchievedCost / (float)graph.OptimalCost) : 1f;
            ratio = Mathf.Clamp01(ratio);

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

            var image = buttonGO.GetComponent<Image>();
            if (image != null)
                image.color = GameData.ColorPalette.edgeColors[colorIndex];

            // Remove previous onClick listeners to avoid duplicates
            var buttonComp = buttonGO.GetComponent<Button>();
            if (buttonComp != null)
            {
                buttonComp.onClick.RemoveAllListeners();
                int capturedIndex = graphIndex; // capture for closure
                buttonComp.onClick.AddListener(() => OnLevelSelected(capturedIndex));
            }
        }
    }

}