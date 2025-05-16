using System;
using System.Collections.Generic;
using UnityEngine;  // For Vector2

[Serializable]
public class Node
{
    public int Id;
    public Vector2 Position;  // Add 2D position for each node
    public int ConnectedComponentId = -1;

    public Node Clone()
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

    public Edge Clone()
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
    public int BestAchievedCost;
    public float Difficulty;
    public string Name;
}

[Serializable]
public class GraphList
{
    public List<Graph> Graphs = new();
}
