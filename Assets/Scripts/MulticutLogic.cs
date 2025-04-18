using System;
using System.Collections.Generic;
using UnityEngine;

public class MulticutLogic : MonoBehaviour
{
    public static void AssignConnectedComponents(Graph graph)
    {
        Dictionary<int, List<int>> adjacencyList = BuildAdjacencyList(graph);
        HashSet<int> visited = new();
        int currentComponentId = 0;

        foreach (var node in graph.Nodes)
        {
            if (!visited.Contains(node.Id))
            {
                DFS(node.Id, currentComponentId, adjacencyList, visited, graph);
                currentComponentId++;
            }
        }
    }

    private static Dictionary<int, List<int>> BuildAdjacencyList(Graph graph)
    {
        var adj = new Dictionary<int, List<int>>();

        foreach (var node in graph.Nodes)
        {
            adj[node.Id] = new List<int>();
        }

        foreach (var edge in graph.Edges)
        {
            if (!edge.IsCut) // Only include non-cut edges in the graph
            {
                adj[edge.FromNodeId].Add(edge.ToNodeId);
                adj[edge.ToNodeId].Add(edge.FromNodeId);
            }
        }

        return adj;
    }

    private static void DFS(int nodeId, int componentId, Dictionary<int, List<int>> adj, HashSet<int> visited, Graph graph)
    {
        visited.Add(nodeId);
        Node node = graph.Nodes.Find(n => n.Id == nodeId);
        if (node != null)
        {
            node.ConnectedComponentId = componentId;
        }

        foreach (int neighbor in adj[nodeId])
        {
            if (!visited.Contains(neighbor))
            {
                DFS(neighbor, componentId, adj, visited, graph);
            }
        }
    }

    // Check if any cut edge connects nodes from different components
    public static bool HasCutBetweenComponents(Graph graph)
    {
        foreach (var edge in graph.Edges)
        {
            if (edge.IsCut)
            {
                var fromNode = graph.Nodes.Find(n => n.Id == edge.FromNodeId);
                var toNode = graph.Nodes.Find(n => n.Id == edge.ToNodeId);

                if (fromNode != null && toNode != null &&
                    fromNode.ConnectedComponentId != toNode.ConnectedComponentId)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
