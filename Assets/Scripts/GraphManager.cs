using UnityEngine;
using System.Collections.Generic;
using TMPro; // Needed for TextMeshPro
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public class GraphManager : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI countdownText;
    private List<GameObject> edges = new List<GameObject>();
    private int currentScore = 0;
    private Dictionary<int, GameObject> nodeIdToGameObjectMap;
    HashSet<int> componentIds = new HashSet<int>();

    void Start()
    {
        if (GameData.SelectedChallenge != null)
        {
            // challenge Mode
            GameData.SelectedGraph = GameData.SelectedChallengeGraphList[GameData.SelectedChallengeGraphIndex];
            countdownText.gameObject.SetActive(true);
            if (GameData.SelectedChallengeGraphIndex == 0)
                GameData.ChallengeStartTime = Time.time;
            levelNameText.text = $"Level\n{GameData.SelectedChallengeGraphIndex + 1}\u00A0|\u00A0{GameData.SelectedChallengeGraphList.Count}";
        }
        else
        {
            countdownText.gameObject.SetActive(false);
        }

        currentScore = GameData.SelectedGraph.BestAchievedCost;
        scoreText.text = $"{-currentScore}/{-GameData.SelectedGraph.OptimalCost}";


        int numComponents = MulticutLogic.AssignConnectedComponents(GameData.SelectedGraph);
        for (int i = 0; i < numComponents; i++)
        {
            componentIds.Add(i);
        }
        // Debug.Log("on start:");
        // Debug.Log(string.Join(", ", componentIds));
        GenerateGraph();
        updateConnectedComponents();
    }

    void Update()
    {
        if (GameData.SelectedChallenge != null)
        {
            // challenge Mode
            updateCountDown();
        }
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
        Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 10f));
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height * 0.9f, 10f));
        float screenWorldWidth = topRight.x - bottomLeft.x;
        float screenWorldHeight = topRight.y - bottomLeft.y;

        // Step 3: Calculate scale factor to fit graph into screen (with slight padding)
        float padding = 0.8f;
        float scaleX, scaleY;
        if (graphWidth != 0)
            scaleX = screenWorldWidth / graphWidth * padding;
        else
            scaleX = 1.0f;

        if (graphHeight != 0)
            scaleY = screenWorldHeight / graphHeight * padding;
        else
            scaleY = 1.0f;
        float scale = Mathf.Min(scaleX, scaleY); // keep aspect ratio

        // Step 4: Centering offset
        Vector2 graphCenter = new Vector2(minX + graphWidth / 2f, minY + graphHeight / 2f);
        Vector2 screenCenter = new Vector2((bottomLeft.x + topRight.x) / 2f, (bottomLeft.y + topRight.y) / 2f);

        // Step 5: Instantiate nodes
        foreach (var node in GameData.SelectedGraph.Nodes)
        {
            // Normalize, scale, and center
            Vector2 localPos = node.Position - graphCenter;
            Vector2 scaledPos = new Vector2(localPos.x * scaleX, localPos.y * scaleY);
            Vector3 worldPos = new Vector3(screenCenter.x + scaledPos.x, screenCenter.y + scaledPos.y, 0f);

            GameObject nodeObj = Instantiate(nodePrefab, worldPos, Quaternion.identity);
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
            // Debug.Log(string.Join(", ", componentIds));
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
        float remainingTime = GameData.SelectedChallenge.TimeLimit + (GameData.SelectedChallengeGraphIndex + 1) * GameData.SelectedChallenge.TimePerLevel - elapsed;

        if (remainingTime <= 0)
        {
            remainingTime = 0;
            SceneManager.LoadScene("ChallengeSelection");
        }

        countdownText.text = $"{remainingTime:F1}s";
        if (remainingTime < 10)
        {
            countdownText.color = Color.red;
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
            if (GameData.SelectedChallenge == null)
            {
                if (currentScore < GameData.SelectedGraph.BestAchievedCost)
                {
                    GameData.SelectedGraph.BestAchievedCost = currentScore;
                    GameData.SaveToPlayerPrefs("graphHighScoreList", GameData.GraphHighScoreList);
                }
            }
            else
            {
                if (currentScore == GameData.SelectedGraph.OptimalCost)
                {
                    // update challenge highscore
                    var highScoreObj = GameData.GetHighScoreForChallenge(GameData.SelectedChallenge);
                    if (GameData.SelectedChallengeGraphIndex + 1 > highScoreObj.HighScore)
                    {
                        highScoreObj.HighScore = GameData.SelectedChallengeGraphIndex + 1;
                        GameData.SaveToPlayerPrefs("challengeHighScoreList", GameData.ChallengeHighScoreList);
                    }

                    // load next challenge level
                    if (GameData.SelectedChallengeGraphIndex == GameData.SelectedChallenge.LevelCount - 1)
                    {
                        SceneManager.LoadScene("ChallengeSelection");
                    }
                    else
                    {
                        GameData.SelectedChallengeGraphIndex += 1;
                        SceneManager.LoadScene("GameScene");
                    }
                }
            }

            scoreText.color = Color.white;
            scoreText.text = $"{-currentScore}/{-GameData.SelectedGraph.OptimalCost}";
        }
        else
        {
            scoreText.color = Color.red;
            scoreText.text = $"{-currentScore}/{-GameData.SelectedGraph.OptimalCost} INVALID!";
        }
    }


    void CreateEdge(GameObject nodeA, GameObject nodeB, Edge edge)
    {
        // Instantiate edge prefab
        GameObject edgeObj = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, this.transform);

        // Set up edge renderer (assuming LineRenderer is set in the prefab)
        EdgeRenderer edgeRenderer = edgeObj.GetComponent<EdgeRenderer>();
        edgeRenderer.graphManager = this;
        edgeRenderer.pointA = nodeA.transform;
        edgeRenderer.pointB = nodeB.transform;
        edgeRenderer.Edge = edge;
    }
}
