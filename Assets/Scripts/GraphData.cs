using System;
using System.Collections.Generic;
using UnityEngine;  // For Vector2

[Serializable]
public class Node
{
    public int Id;
    public Vector2 Position;  // Add 2D position for each node
    public int ConnectedComponentId = -1;
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
