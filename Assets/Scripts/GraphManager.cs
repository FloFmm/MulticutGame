using UnityEngine;
using System.Collections.Generic;
using TMPro; // Needed for TextMeshPro
using System;
using System.Linq;

public class GraphManager : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public TextMeshProUGUI scoreText;
    private List<GameObject> edges = new List<GameObject>();
    private GraphList CurrentGraphList;
    public Graph currentGraph;
    private int currentScore = 0;
    private Dictionary<int, GameObject> nodeIdToGameObjectMap;
    HashSet<int> componentIds = new HashSet<int>();
    void Start()
    {
        
        CurrentGraphList = GraphStorage.LoadGraphs();
        if (CurrentGraphList.Graphs.Count > 0)
        {
            string selectedGraphName = GameData.SelectedGraphName;
            currentGraph = CurrentGraphList.Graphs.FirstOrDefault(graph => graph.Name == selectedGraphName);
            currentScore = currentGraph.BestAchievedCost;
            scoreText.text = $"0 / {-currentGraph.OptimalCost}";
            int numComponents = MulticutLogic.AssignConnectedComponents(currentGraph);
            for (int i = 0; i < numComponents; i++)
            {
                componentIds.Add(i);
            }
            GenerateGraph(); // Use the first graph in the list
            updateConnectedComponents();
        }
        else
        {
            Debug.LogError("No graphs found in the loaded data.");
        }
    }

    // public List<GameObject> GetEdges() => edges;void Start()
    void GenerateGraph()
    {
        nodeIdToGameObjectMap = new Dictionary<int, GameObject>();
        // Step 1: Determine bounds of graph
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var node in currentGraph.Nodes)
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
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height*0.9f, 10f));
        float screenWorldWidth = topRight.x - bottomLeft.x;
        float screenWorldHeight = topRight.y - bottomLeft.y;

        // Step 3: Calculate scale factor to fit graph into screen (with slight padding)
        float padding = 0.9f;
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
        foreach (var node in currentGraph.Nodes)
        {
            // Normalize, scale, and center
            Vector2 localPos = node.Position - graphCenter;
            Vector2 scaledPos = new Vector2(localPos.x * scaleX, localPos.y * scaleY);
            Vector3 worldPos = new Vector3(screenCenter.x + scaledPos.x, screenCenter.y + scaledPos.y, 0f);

            GameObject nodeObj = Instantiate(nodePrefab, worldPos, Quaternion.identity);
            nodeIdToGameObjectMap[node.Id] = nodeObj;
        }

        // Step 6: Instantiate edges
        foreach (var edge in currentGraph.Edges)
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
            Graph subgraph = MulticutLogic.FilterGraphByComponentIds(currentGraph, new List<int> {id1, id2});
            int numComponentsSubgraph = MulticutLogic.AssignConnectedComponents(subgraph);
            if (numComponentsSubgraph == 1 && id1!=id2)
            {
                // two components were joined
                foreach (var node in subgraph.Nodes)
                {
                    node.ConnectedComponentId = id1;
                    componentIds.Remove(id2);
                }
            }
            else if (numComponentsSubgraph == 2 && id1 == id2)
            {
                // two components were seperated
                // smallest id that is available 
                int candidate = 0;
                while (componentIds.Contains(candidate))
                    candidate++;
                componentIds.Add(candidate);
                foreach (var node in subgraph.Nodes)
                {
                    if (node.ConnectedComponentId == 0)
                        node.ConnectedComponentId = id1;
                    else
                    {
                        if (id1!=id2)
                            node.ConnectedComponentId = id2;
                        else
                        {
                            node.ConnectedComponentId = candidate;
                            
                        }
                    }
                }
            }
            else if (numComponentsSubgraph != 1 && numComponentsSubgraph != 2)
                throw new ArgumentException($"There should be 1 or 2 components in subgraph, not: {numComponentsSubgraph}", nameof(numComponentsSubgraph));

        }
        
        foreach (var node in currentGraph.Nodes)
        {
            GameObject nodeObj = nodeIdToGameObjectMap[node.Id];
            NodeRenderer nodeRenderer = nodeObj.GetComponent<NodeRenderer>();
            if (nodeRenderer.ConnectedComponentId != node.ConnectedComponentId)
                nodeRenderer.ConnectedComponentId = node.ConnectedComponentId;
        }
        Debug.Log("Set contains: " + string.Join(", ", componentIds));
    }

    public bool isValidMulticut()
    {
        foreach (var edge in currentGraph.Edges)
        {
            if (edge.IsCut)
            {
                int id1 = nodeIdToGameObjectMap[edge.FromNodeId].GetComponent<NodeRenderer>().ConnectedComponentId;
                int id2 = nodeIdToGameObjectMap[edge.ToNodeId].GetComponent<NodeRenderer>().ConnectedComponentId;
                if (id1 == id2)
                {
                    // Debug.Log(nodeIdToGameObjectMap[edge.FromNodeId].GetComponent<NodeRenderer>());
                    Debug.Log(id1);
                    return false;
                }
            }
        }
        return true;
    }

    public void updateScoreText(bool isCut, int cost)
    {
        if (isCut)
            currentScore += cost;
        else
            currentScore -= cost;

        
        if (isValidMulticut())
        {
            scoreText.color = Color.white;
            scoreText.text = $"{-currentScore} / {-currentGraph.OptimalCost}";
        }
        else
        {
            scoreText.color = Color.red;
            scoreText.text = $"{-currentScore} / {-currentGraph.OptimalCost} INVALID!";
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
