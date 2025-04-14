using UnityEngine;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public int nodeCount = 10;
    private List<GameObject> nodes = new List<GameObject>();
    private List<GameObject> edges = new List<GameObject>();
    public GraphList CurrentGraphList;

    void Start()
    {
        CurrentGraphList = GraphStorage.LoadGraphs();
        if (CurrentGraphList.Graphs.Count > 0)
        {
            GenerateGraph(CurrentGraphList.Graphs[0]); // Use the first graph in the list
        }
        else
        {
            Debug.LogError("No graphs found in the loaded data.");
        }
    }

    // void GenerateGraph()
    // {
    //     float screenWidth = Screen.width;
    //     float screenHeight = Screen.height;
    //     for (int i = 0; i < nodeCount; i++)
    //     {
    //         // Randomly generate positions within screen bounds.
    //         Vector2 pos = new Vector2(Random.Range(0, screenWidth), Random.Range(0, screenHeight)); // Random positions within screen size
            
    //         // Convert to world position if necessary (use Camera.main.ScreenToWorldPoint if needed for world space)
    //         Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos.x, pos.y, 10f));  // Assuming 10f as Z-axis depth
    //         worldPos.z = 0f; // If you're working in 2D, set Z to 0

    //         GameObject node = Instantiate(nodePrefab, worldPos, Quaternion.identity);
    //         nodes.Add(node);
    //     }

    //     // Create edges between nodes (randomly)
    //     for (int i = 0; i < nodes.Count; i++)
    //     {
    //         for (int j = i + 1; j < nodes.Count; j++)
    //         {
    //             if (Random.value < 1.0f) // 30% chance of connecting nodes
    //             {
    //                 CreateEdge(nodes[i], nodes[j]);
    //             }
    //         }
    //     }
    // }

    // void CreateEdge(GameObject nodeA, GameObject nodeB)
    // {
    //     for (int i = 0; i < nodes.Count - 1; i++)
    //         {
    //             GameObject edge = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, this.transform);
    //             EdgeRenderer edgeRenderer = edge.GetComponent<EdgeRenderer>();
    //             edgeRenderer.pointA = nodes[i].transform;
    //             edgeRenderer.pointB = nodes[i + 1].transform;

    //             // You can optionally fetch the LineRenderer if it's not assigned in prefab
    //             if (edgeRenderer.lineRenderer == null)
    //                 edgeRenderer.lineRenderer = edge.GetComponent<LineRenderer>();
    //         }
    // }

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



    void CreateEdge(GameObject nodeA, GameObject nodeB, int cost, bool isCut, bool optimalCut)
    {
        // Instantiate edge prefab
        GameObject edge = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, this.transform);

        // Set up edge renderer (assuming LineRenderer is set in the prefab)
        EdgeRenderer edgeRenderer = edge.GetComponent<EdgeRenderer>();
        edgeRenderer.pointA = nodeA.transform;
        edgeRenderer.pointB = nodeB.transform;

        // Optionally, set edge properties (e.g., cost, isCut, optimalCut) on the renderer or any related script
        // edgeRenderer.SetEdgeProperties(cost, isCut, optimalCut);
        edgeRenderer.Cost = cost;
        edgeRenderer.IsCut = isCut;
        edgeRenderer.OptimalCut = optimalCut;
    }
}
