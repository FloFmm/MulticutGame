using UnityEngine;
using System.Collections.Generic;
using TMPro; // Needed for TextMeshPro
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public class GraphManager : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public GameObject levelOverlay;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI countdownText;
    public RectTransform graphContainingRect;
    private List<GameObject> edges = new List<GameObject>();
    private int currentScore = 0;
    private Dictionary<int, GameObject> nodeIdToGameObjectMap;


    HashSet<int> componentIds = new HashSet<int>();

    void Start()
    {
        GameData.LoadLevelOrChallenge();
        levelOverlay.SetActive(false);
        if (GameData.SelectedChallenge != null)
        {
            // challenge Mode
            levelText.gameObject.SetActive(false);
            countdownText.gameObject.SetActive(true);
            if (GameData.SelectedChallengeGraphIndex == 0)
                GameData.ChallengeStartTime = Time.time;
            levelNameText.text = $"LEVEL\n{GameData.SelectedChallengeGraphIndex + 1}\u00A0|\u00A0{GameData.SelectedChallengeGraphList.Count}";
        }
        else
        {
            // tutorial or level
            if (GameData.IsTutorial)
            {
                levelText.gameObject.SetActive(true);
                levelText.GetComponent<ClickableText>().messages = GameData.SelectedGraph.Text;
                levelText.GetComponent<ClickableText>().UpdateText();
                levelNameText.text = $"LEVEL\n{GameData.SelectedGraphIndex + 1}\u00A0|\u00A0{GameData.TutorialList.Graphs.Count}";
            }
            else
            {
                levelText.gameObject.SetActive(false);
                levelNameText.text = $"LEVEL\n{GameData.SelectedGraphIndex + 1}\u00A0|\u00A0{GameData.GraphHighScoreList.Graphs.Count}";
            }
            countdownText.gameObject.SetActive(false);
        }

        currentScore = GameData.SelectedGraph.CalculateCurrentCost();
        scoreText.text = $"{-currentScore}/{-GameData.SelectedGraph.OptimalCost}";


        int numComponents = MulticutLogic.AssignConnectedComponents(GameData.SelectedGraph);
        for (int i = 0; i < numComponents; i++)
        {
            componentIds.Add(i);
        }
        GenerateGraph();
        updateConnectedComponents();

        if (currentScore == GameData.SelectedGraph.OptimalCost)
            ActivateOverlay(() => loadNextLevel());
    }

    void Update()
    {
        if (GameData.SelectedChallenge != null)
        {
            // challenge Mode
            updateCountDown();
        }
    }

    private Rect GetGraphContainingRect()
    {
        Vector3[] corners = new Vector3[4];
        graphContainingRect.GetWorldCorners(corners);
        // corners: 0=bottom-left, 2=top-right
        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];
        // If the levelText is NOT active, expand the rect downward by 300 UI pixels converted to world units
        if (!levelText.gameObject.activeInHierarchy)
        {
            // Convert 300 UI pixels to world units based on canvas scale
            float pixelOffset = 250f;

            // Use camera to convert screen delta to world delta
            Vector3 worldOffset = Camera.main.ScreenToWorldPoint(new Vector3(0, pixelOffset, 0)) -
                                Camera.main.ScreenToWorldPoint(Vector3.zero);

            bottomLeft.y -= worldOffset.y;
        }
        return new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
    }


    void GenerateGraph()
    {
        nodeIdToGameObjectMap = new Dictionary<int, GameObject>();
        // Step 1: Determine bounds of graph
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var node in GameData.SelectedGraph.Nodes)
        {
            if (node.Position.x < minX) minX = node.Position.x;
            if (node.Position.x > maxX) maxX = node.Position.x;
            if (node.Position.y < minY) minY = node.Position.y;
            if (node.Position.y > maxY) maxY = node.Position.y;
        }

        float graphWidth = maxX - minX;
        float graphHeight = maxY - minY;

        // Step 2: Determine screen size in world units
        Rect layoutRect = GetGraphContainingRect();
        float screenWorldWidth = layoutRect.width;
        float screenWorldHeight = layoutRect.height;
        Vector2 screenCenter = new Vector2(layoutRect.x + layoutRect.width / 2f, layoutRect.y + layoutRect.height / 2f);

        // Step 3: Calculate scale factor to fit graph into screen
        float scaleX, scaleY;
        if (graphWidth != 0)
            scaleX = screenWorldWidth / graphWidth;
        else
            scaleX = 1.0f;

        if (graphHeight != 0)
            scaleY = screenWorldHeight / graphHeight;
        else
            scaleY = 1.0f;
        float scale = Mathf.Min(scaleX, scaleY); // keep aspect ratio

        // Step 4: Centering offset
        Vector2 graphCenter = new Vector2(minX + graphWidth / 2f, minY + graphHeight / 2f);

        // Step 5: Instantiate nodes
        foreach (var node in GameData.SelectedGraph.Nodes)
        {
            // Normalize, scale, and center
            Vector2 localPos = node.Position - graphCenter;
            Vector2 scaledPos = new Vector2(localPos.x * scaleX, localPos.y * scaleY);
            Vector3 worldPos = new Vector3(screenCenter.x + scaledPos.x, screenCenter.y + scaledPos.y, 0f);

            GameObject nodeObj = Instantiate(nodePrefab, worldPos, Quaternion.identity, this.transform);
            //Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, this.transform);
            nodeIdToGameObjectMap[node.Id] = nodeObj;
        }

        // Step 6: Instantiate edges
        foreach (var edge in GameData.SelectedGraph.Edges)
        {
            GameObject nodeA = nodeIdToGameObjectMap[edge.FromNodeId];
            GameObject nodeB = nodeIdToGameObjectMap[edge.ToNodeId];
            CreateEdge(nodeA, nodeB, edge);
        }
    }

    public void updateConnectedComponents(Edge edge = null)
    {
        if (edge != null)
        {
            int id1 = nodeIdToGameObjectMap[edge.FromNodeId].GetComponent<NodeRenderer>().ConnectedComponentId;
            int id2 = nodeIdToGameObjectMap[edge.ToNodeId].GetComponent<NodeRenderer>().ConnectedComponentId;
            // id1 must be smaller than id2
            if (id1 > id2)
            {
                int tmp = id1;
                id1 = id2;
                id2 = tmp;
            }
            Graph subgraph = MulticutLogic.FilterGraphByComponentIds(GameData.SelectedGraph, new List<int> { id1, id2 });
            int numComponentsSubgraph = MulticutLogic.AssignConnectedComponents(subgraph);
            if (numComponentsSubgraph == 1)
            {

                if (id1 != id2)
                {
                    // two components were joined
                    componentIds.Remove(id2);
                }

                foreach (var node in subgraph.Nodes)
                {
                    node.ConnectedComponentId = id1;
                }
            }
            else if (numComponentsSubgraph == 2)
            {
                int candidate = 0;
                if (id1 == id2)
                {
                    // two components were seperated
                    // smallest id that is available 
                    while (componentIds.Contains(candidate))
                        candidate++;
                    componentIds.Add(candidate);
                }
                foreach (var node in subgraph.Nodes)
                {
                    if (node.ConnectedComponentId == 0)
                        node.ConnectedComponentId = id1;
                    else
                    {
                        if (id1 != id2)
                            node.ConnectedComponentId = id2;
                        else
                        {
                            // two components were seperated
                            node.ConnectedComponentId = candidate;
                        }
                    }
                }
            }
            else if (numComponentsSubgraph != 1 && numComponentsSubgraph != 2)
                throw new ArgumentException($"There should be 1 or 2 components in subgraph, not: {numComponentsSubgraph}", nameof(numComponentsSubgraph));
        }

        foreach (var node in GameData.SelectedGraph.Nodes)
        {
            GameObject nodeObj = nodeIdToGameObjectMap[node.Id];
            NodeRenderer nodeRenderer = nodeObj.GetComponent<NodeRenderer>();
            if (nodeRenderer.ConnectedComponentId != node.ConnectedComponentId)
                nodeRenderer.ConnectedComponentId = node.ConnectedComponentId;
        }
    }

    public bool isValidMulticut()
    {
        foreach (var edge in GameData.SelectedGraph.Edges)
        {
            if (edge.IsCut)
            {
                int id1 = nodeIdToGameObjectMap[edge.FromNodeId].GetComponent<NodeRenderer>().ConnectedComponentId;
                int id2 = nodeIdToGameObjectMap[edge.ToNodeId].GetComponent<NodeRenderer>().ConnectedComponentId;
                if (id1 == id2)
                    return false;
            }
        }
        return true;
    }

    public void updateCountDown()
    {
        float elapsed = Time.time - GameData.ChallengeStartTime;
        float remainingTime = GameData.SelectedChallenge.TimeLimit + GameData.SelectedChallengeGraphIndex * GameData.SelectedChallenge.TimePerLevel - elapsed;

        if (remainingTime <= 0)
        {
            remainingTime = 0;
            SceneManager.LoadScene("ChallengeSelection");
        }

        countdownText.text = $"{remainingTime:F1}s";
        if (remainingTime < 10)
        {
            countdownText.color = GameData.ColorPalette.lowRemainingTimeColor;
        }
        else
        {
            countdownText.color = Color.white;
        }
    }

    public void updateScoreText(bool isCut, int cost)
    {
        if (isCut)
            currentScore += cost;
        else
            currentScore -= cost;


        if (isValidMulticut())
        {
            scoreText.text = $"{-currentScore}/{-GameData.SelectedGraph.OptimalCost}";
            if (currentScore < GameData.SelectedGraph.BestAchievedCost)
            {
                if (levelOverlay.activeSelf)
                {
                    levelOverlay.SetActive(false);
                }
                if (GameData.SelectedChallenge != null) //challenge
                {
                }
                else if (GameData.IsTutorial) // tutorial
                {
                    GameData.SelectedGraph.BestAchievedCost = currentScore;
                }
                else // normal level
                {
                    GameData.SelectedGraph.BestAchievedCost = currentScore;
                    GameData.SaveToPlayerPrefs("graphHighScoreList", GameData.GraphHighScoreList);
                }
            }
            if (currentScore == GameData.SelectedGraph.OptimalCost)
            {
                scoreText.color = GameData.ColorPalette.optimalSolutionColor;

                if (GameData.SelectedChallenge != null) //challenge
                {
                    // update challenge highscore
                    var highScoreObj = GameData.GetHighScoreForChallenge(GameData.SelectedChallenge);
                    if (GameData.SelectedChallengeGraphIndex + 1 > highScoreObj.HighScore)
                    {
                        highScoreObj.HighScore = GameData.SelectedChallengeGraphIndex + 1;
                        GameData.SaveToPlayerPrefs("challengeHighScoreList", GameData.ChallengeHighScoreList);
                    }
                }
                else if (GameData.IsTutorial) // tutorial
                {
                }
                else // normal level
                {
                }
                ActivateOverlay(() => loadNextLevel());
            }
            else
            {
                if (levelOverlay.activeSelf)
                {
                    levelOverlay.SetActive(false);
                }
                scoreText.color = GameData.ColorPalette.normalTextColor;
            }
        }
        else
        {
            if (levelOverlay.activeSelf)
            {
                levelOverlay.SetActive(false);
            }
            scoreText.color = GameData.ColorPalette.invalidSolutionColor;
            scoreText.text = $"{-currentScore}/{-GameData.SelectedGraph.OptimalCost} INVALID!";
        }
    }


    void CreateEdge(GameObject nodeA, GameObject nodeB, Edge edge)
    {
        // Instantiate edge prefab
        GameObject edgeObj = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, this.transform);
        edges.Add(edgeObj);
        // Set up edge renderer (assuming LineRenderer is set in the prefab)
        EdgeRenderer edgeRenderer = edgeObj.GetComponent<EdgeRenderer>();
        edgeRenderer.graphManager = this;
        edgeRenderer.pointA = nodeA.transform;
        edgeRenderer.pointB = nodeB.transform;
        edgeRenderer.Edge = edge;
    }

    public void ActivateOverlay(UnityAction buttonAction)
    {
        string text = "LEVEL COMPLETE";
        string buttonText = "NEXT LEVEL";
        // Find the main text by name
        levelOverlay.SetActive(true);
        TMP_Text mainText = levelOverlay.transform.Find("Text")?.GetComponent<TMP_Text>();
        if (mainText != null)
        {
            mainText.text = text;
            mainText.color = GameData.ColorPalette.LevelCompleteTextColor;
        }

        //Find the button by name
        Transform buttonTransform = levelOverlay.transform.Find("Button");
        if (buttonTransform != null)
        {
            Button button = buttonTransform.GetComponent<Button>();
            TMP_Text buttonLabel = buttonTransform.GetComponentInChildren<TMP_Text>();

            if (button != null && buttonLabel != null)
            {
                var image = button.GetComponent<Image>();
                image.color = GameData.ColorPalette.LevelCompleteButtonColor;
                buttonLabel.text = buttonText;
                button.onClick.AddListener(buttonAction);
            }
        }
    }

    public void loadNextLevel()
    {
        if (GameData.SelectedChallenge != null) //challenge
        {
            GameData.SelectedChallengeGraphIndex += 1;
            if (GameData.SelectedChallengeGraphIndex >= GameData.SelectedChallenge.LevelCount)
            {
                SceneManager.LoadScene("ChallengeSelection");
                return;
            }
            SceneManager.LoadScene("GameScene");
            return;
        }
        else if (GameData.IsTutorial) // tutorial
        {
            GameData.SelectedGraphIndex += 1;
            if (GameData.SelectedGraphIndex >= GameData.TutorialList.Graphs.Count)
            {
                SceneManager.LoadScene("LevelSelection");
                return;
            }
            SceneManager.LoadScene("GameScene");
            return;
        }
        else // normal level
        {
            int offset = 1;
            foreach (Graph graph in GameData.GraphHighScoreList.Graphs.Skip(GameData.SelectedGraphIndex + 1))
            {
                if (graph.BestAchievedCost != graph.OptimalCost)
                {
                    GameData.SelectedGraphIndex += offset;
                    SceneManager.LoadScene("GameScene");
                    return; // Stop further execution
                }
                offset++;
            }
            SceneManager.LoadScene("LevelSelection");
        }
    }


    public List<GameObject> getEdges()
    {
        return edges;
    }
}
