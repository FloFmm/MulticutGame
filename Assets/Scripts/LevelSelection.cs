using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LevelSelection : MonoBehaviour {
    public GameObject buttonPrefab; // Assign MenuButtonPrefab in inspector
    public Transform contentParent; // Assign Content of Scroll View
    public List<Color> availableColors; // Assign colors in inspector (should have 5 for 5 buckets)
    void Start() {
        foreach (Graph graph in GameData.GraphList.Graphs) {
            GameObject newButton = Instantiate(buttonPrefab, contentParent);
            newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = graph.Name;
            
            // Compute ratio
            float ratio = (graph.OptimalCost != 0f) ? (graph.BestAchievedCost / (float)graph.OptimalCost) : 1f;
            ratio = Mathf.Clamp01(ratio); // Ensure between 0 and 1

            // Determine color bucket
            int colorIndex = Mathf.FloorToInt(ratio * (availableColors.Count-1));
            colorIndex = Mathf.Clamp(colorIndex, 0, availableColors.Count - 1);

            // Apply color (to Image component on the button)
            var image = newButton.GetComponent<Image>();
            if (image != null)
                image.color = availableColors[colorIndex];
            
            // Optional: add a click handler
            newButton.GetComponent<Button>().onClick.AddListener(() => OnLevelSelected(graph));
        }
    }

    void OnLevelSelected(Graph graph) {
        GameData.SelectedGraph = graph;
        SceneManager.LoadScene("GameScene");
    }
}