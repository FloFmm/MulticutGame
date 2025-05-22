using System;
using System.Collections.Generic;
using UnityEngine;  // For Vector2

[Serializable]
public class Node
{
    public int Id;
    public Vector2 Position;  // Add 2D position for each node
    public int ConnectedComponentId = -1;

    public Node Copy()
    {
        return new Node
        {
            Id = this.Id,
            Position = this.Position,
            ConnectedComponentId = this.ConnectedComponentId
        };
    }
}

[Serializable]
public class Edge
{
    public int FromNodeId;
    public int ToNodeId;
    public int Cost;
    public bool IsCut;
    public bool OptimalCut;
    public bool IsSpecial = false;

    public Edge Copy()
    {
        return new Edge
        {
            FromNodeId = this.FromNodeId,
            ToNodeId = this.ToNodeId,
            Cost = this.Cost,
            IsCut = this.IsCut,
            OptimalCut = this.OptimalCut,
            IsSpecial = this.IsSpecial
        };
    }
}

[Serializable]
public class Graph
{
    public List<Node> Nodes = new();
    public List<Edge> Edges = new();
    public int OptimalCost;
    public int BestAchievedCost = 0;
    public float Difficulty;
    public string Name;
    public string CreatedAt;

    public Graph DeepCopy()
    {
        Graph graphCopy = new Graph
        {
            OptimalCost = OptimalCost,
            BestAchievedCost = BestAchievedCost,
            Difficulty = Difficulty,
            Name = Name,
            CreatedAt = CreatedAt
        };

        foreach (var node in Nodes)
        {
            graphCopy.Nodes.Add(node.Copy());
        }

        foreach (var edge in Edges)
        {
            graphCopy.Edges.Add(edge.Copy());
        }

        return graphCopy;
    }
}

[Serializable]
public class GraphList
{
    public List<Graph> Graphs = new();

    public GraphList DeepCopy()
    {
        GraphList copy = new GraphList();

        foreach (var graph in this.Graphs)
        {
            copy.Graphs.Add(graph.DeepCopy());
        }

        return copy;
    }
}
