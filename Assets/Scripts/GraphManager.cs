using UnityEngine;
using System.Collections.Generic;
using TMPro; // Needed for TextMeshPro

public class GraphManager : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public TextMeshProUGUI scoreText;
    private List<GameObject> nodes = new List<GameObject>();
    private List<GameObject> edges = new List<GameObject>();
    private GraphList CurrentGraphList;
    private Graph currentGraph;
    private int currentScore = 0;

    void Start()
    {
        CurrentGraphList = GraphStorage.LoadGraphs();
        if (CurrentGraphList.Graphs.Count > 0)
        {
            currentScore = 0;
            currentGraph = CurrentGraphList.Graphs[0];
            scoreText.text = $"0 / {currentGraph.OptimalCost}";
            MulticutLogic.AssignConnectedComponents(currentGraph);
            GenerateGraph(currentGraph); // Use the first graph in the list
        }
        else
        {
            Debug.LogError("No graphs found in the loaded data.");
        }
    }

    // public List<GameObject> GetEdges() => edges;void Start()
    void GenerateGraph(Graph graph)
    {
        // Step 1: Determine bounds of graph
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var node in graph.Nodes)
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
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 10f));
        float screenWorldWidth = topRight.x - bottomLeft.x;
        float screenWorldHeight = topRight.y - bottomLeft.y;

        // Step 3: Calculate scale factor to fit graph into screen (with slight padding)
        float padding = 0.9f;
        float scaleX = screenWorldWidth / graphWidth * padding;
        float scaleY = screenWorldHeight / graphHeight * padding;
        float scale = Mathf.Min(scaleX, scaleY); // keep aspect ratio

        // Step 4: Centering offset
        Vector2 graphCenter = new Vector2(minX + graphWidth / 2f, minY + graphHeight / 2f);
        Vector2 screenCenter = new Vector2((bottomLeft.x + topRight.x) / 2f, (bottomLeft.y + topRight.y) / 2f);

        // Step 5: Instantiate nodes
        foreach (var node in graph.Nodes)
        {
            // Normalize, scale, and center
            Vector2 localPos = node.Position - graphCenter;
            Vector2 scaledPos = localPos * scale;
            Vector3 worldPos = new Vector3(screenCenter.x + scaledPos.x, screenCenter.y + scaledPos.y, 0f);

            GameObject nodeObj = Instantiate(nodePrefab, worldPos, Quaternion.identity);
            NodeRenderer nodeRenderer = nodeObj.GetComponent<NodeRenderer>();
            // nodeRenderer.ConnectedComponentId = 1;
            // renderer.material.color;
            nodes.Add(nodeObj);
        }

        // Step 6: Instantiate edges
        foreach (var edge in graph.Edges)
        {
            GameObject nodeA = nodes[edge.FromNodeId];
            GameObject nodeB = nodes[edge.ToNodeId];
            CreateEdge(nodeA, nodeB, edge.Cost, edge.IsCut, edge.OptimalCut);
        }
    }

    public void updateScoreText(bool isCut, int cost)
    {
        if (isCut)
            currentScore += cost;
        else
            currentScore -= cost;
        scoreText.text = $"{currentScore} / {currentGraph.OptimalCost}";
    }


    void CreateEdge(GameObject nodeA, GameObject nodeB, int cost, bool isCut, bool optimalCut)
    {
        // Instantiate edge prefab
        GameObject edge = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, this.transform);

        // Set up edge renderer (assuming LineRenderer is set in the prefab)
        EdgeRenderer edgeRenderer = edge.GetComponent<EdgeRenderer>();
        edgeRenderer.graphManager = this;
        edgeRenderer.pointA = nodeA.transform;
        edgeRenderer.pointB = nodeB.transform;
        edgeRenderer.Cost = cost;
        edgeRenderer.IsCut = isCut;
        edgeRenderer.OptimalCut = optimalCut;
    }
}
