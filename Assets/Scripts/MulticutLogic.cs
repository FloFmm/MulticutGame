using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class MulticutLogic : MonoBehaviour
{
    public static Graph FilterGraphByComponentIds(Graph originalGraph, List<int> componentIds)
    {
        // Step 1: Filter nodes (which either are in component one of the given components)
        var filteredNodes = originalGraph.Nodes
            .Where(node => componentIds.Contains(node.ConnectedComponentId))
            .ToList();

        // Create a hash set of the filtered node IDs for fast lookup
        var validNodeIds = new HashSet<int>(filteredNodes.Select(n => n.Id));

        // Step 2: Filter edges that connect two nodes in the filtered node list
        var filteredEdges = originalGraph.Edges
            .Where(edge => validNodeIds.Contains(edge.FromNodeId) && validNodeIds.Contains(edge.ToNodeId))
            .ToList();

        // Step 3: Create new graph
        Graph filteredGraph = new Graph
        {
            Nodes = filteredNodes,
            Edges = filteredEdges,
            OptimalCost = originalGraph.OptimalCost
        };

        return filteredGraph;
    }
    
    
    public static int AssignConnectedComponents(Graph graph)
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
        return currentComponentId;
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
