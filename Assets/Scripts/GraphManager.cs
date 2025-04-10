using UnityEngine;
using System.Collections.Generic;

public class GraphManager : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public int nodeCount = 10;

    private List<GameObject> nodes = new List<GameObject>();
    private List<GameObject> edges = new List<GameObject>();

    void Start()
    {
        GenerateGraph();
    }

    void GenerateGraph()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        for (int i = 0; i < nodeCount; i++)
        {
            // Randomly generate positions within screen bounds.
            Vector2 pos = new Vector2(Random.Range(0, screenWidth), Random.Range(0, screenHeight)); // Random positions within screen size
            
            // Convert to world position if necessary (use Camera.main.ScreenToWorldPoint if needed for world space)
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(pos.x, pos.y, 10f));  // Assuming 10f as Z-axis depth
            worldPos.z = 0f; // If you're working in 2D, set Z to 0

            GameObject node = Instantiate(nodePrefab, worldPos, Quaternion.identity);
            nodes.Add(node);
        }

        // Create edges between nodes (randomly)
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                if (Random.value < 1.0f) // 30% chance of connecting nodes
                {
                    CreateEdge(nodes[i], nodes[j]);
                }
            }
        }
    }

    void CreateEdge(GameObject nodeA, GameObject nodeB)
    {
        for (int i = 0; i < nodes.Count - 1; i++)
            {
                GameObject edge = Instantiate(edgePrefab, Vector3.zero, Quaternion.identity, this.transform);
                EdgeRenderer edgeRenderer = edge.GetComponent<EdgeRenderer>();
                edgeRenderer.pointA = nodes[i].transform;
                edgeRenderer.pointB = nodes[i + 1].transform;

                // You can optionally fetch the LineRenderer if it's not assigned in prefab
                if (edgeRenderer.lineRenderer == null)
                    edgeRenderer.lineRenderer = edge.GetComponent<LineRenderer>();
            }
    }

    public List<GameObject> GetEdges() => edges;
}
