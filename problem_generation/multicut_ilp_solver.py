import gurobipy as gp
import networkx as nx
import numpy as np
import json
from gurobipy import GRB
from typing import List
from networkx.drawing.nx_agraph import graphviz_layout


def solve_multicut(graph: nx.Graph, costs: dict, log: bool = True):
    """
    This method solves the minimum cost multicut problem for the given graph and edge costs.
    :param graph: undirected simple graph.
    :param costs: dict that assigns a cost to each edge in the graph.
    :param log: flag indicating whether gurobi should print out the log
    :return: dict that assigns a 0-1 labeling to the edges where 1 indicates that the edge is cut.
    """

    # create IPL model
    model = gp.Model()
    model.setParam("OutputFlag", 1 if log else 0)
    # add the variables to the model
    variables = model.addVars(costs.keys(), obj=costs, vtype=GRB.BINARY, name="e")
    for i, j in list(variables.keys()):
        variables[j, i] = variables[i, j]

    # define the algorithm for separating cycle inequalities
    def separate_cycle_inequalities(_, where):
        # if the solver did not find a new integral solution, do nothing
        if where != GRB.Callback.MIPSOL:
            return
        # extract the values of the current solution
        vals = model.cbGetSolution(variables)
        # compute the connected components with respect to the current solution
        g_copy = graph.copy()
        g_copy.remove_edges_from([e for e in g_copy.edges if vals[e] > 0.5])
        components = nx.connected_components(g_copy)
        node_labeling = dict()
        for i, comp in enumerate(components):
            for n in comp:
                node_labeling[n] = i
        # iterate over all edges
        for (u, v), x_uv in vals.items():
            # if the edge uv has value x_uv = 1 but node_labeling[u] == node_labeling[v] then there exists
            # a violated cycle constrained for the edge uv. Otherwise, there is no violated inequality.
            if x_uv < 0.5 or node_labeling[u] != node_labeling[v]:
                continue
            # search for the shortest such violated path and add the corresponding inequality to the model
            path = nx.shortest_paths.shortest_path(g_copy, u, v)
            assert len(path) >= 2
            model.cbLazy(
                variables[u, v]
                <= gp.quicksum(
                    variables[path[i], path[i + 1]] for i in range(len(path) - 1)
                )
            )

    # optimize the model
    model.Params.LazyConstraints = 1
    model.optimize(separate_cycle_inequalities)

    # return the 0-1 edge labeling by rounding the solution
    solution = model.getAttr("X", variables)
    multicut = {e: 1 if x_e > 0.5 else 0 for e, x_e in solution.items()}
    return multicut, model.ObjVal


def generate_graphs_and_multicuts(
    n: int,
    graph_size: int,
    output_path: str,
    cost_probs: List[float],
    available_costs: List[int],
    edge_prob: float,
):
    """
    Generate n graphs and solve the multicut problem for each.
    Save the results as a JSON file.
    :param n: number of graphs to generate.
    :param graph_size: number of nodes per graph.
    """
    graphs_data = []

    for _ in range(n):
        # Generate random positions for nodes
        # positions = {
        #     i: (np.random.random(), np.random.random()) for i in range(graph_size)
        # }
        graph = nx.Graph()
        graph.add_nodes_from(range(graph_size))

        # Add random edges between nodes
        edges = []
        for i in graph.nodes:
            for j in graph.nodes:
                if (
                    i < j and np.random.random() < edge_prob
                ):  # 80% chance to connect any two nodes
                    # edges.append((i, j))
                    graph.add_edge(i, j, minlen=300)
        # graph.add_edges_from(edges)

        # Check if the graph is connected, if not, add edges to connect it
        if not nx.is_connected(graph):
            components = list(nx.connected_components(graph))
            while not nx.is_connected(graph):
                # Get the first two components
                comp1, comp2 = components[-2], components[-1]
                # Find the first pair of nodes from different components
                u, v = list(comp1)[0], list(comp2)[0]
                # Add an edge to connect them
                graph.add_edge(u, v, minlen=300)
                components = list(nx.connected_components(graph))

        positions = graphviz_layout(graph, prog="neato")
        nx.set_node_attributes(graph, positions, "pos")

        # Generate random edge costs
        np.random.seed(2)
        bias = 0.3
        costs = {}
        for u, v in graph.edges():
            cost_index = np.random.choice(len(cost_probs), p=cost_probs)
            costs[u, v] = available_costs[cost_index]

        # Solve the multicut problem for the graph
        multicut, obj = solve_multicut(graph, costs)

        # Prepare data for C# serialization
        graph_data = {
            "Nodes": [
                {"Id": n, "Position": {"x": pos[0], "y": pos[1]}}
                for n, pos in positions.items()
            ],
            "Edges": [
                {
                    "FromNodeId": u,
                    "ToNodeId": v,
                    "Cost": costs[u, v],
                    "IsCut": False,
                    "OptimalCut": (True if multicut.get((u, v), 0) == 1 else False),
                }
                for u, v in graph.edges()
            ],
            "OptimalCost": obj,
        }
        graphs_data.append(graph_data)

    # Save to a JSON file
    with open(output_path, "w") as f:
        json.dump({"Graphs": graphs_data}, f, indent=4)


def main():
    output_path = "Assets/Resources/graphs.json"
    n = 5  # number of graphs to generate
    graph_size = 15  # number of nodes per graph
    # TODO remove nodes that are:
    # 1) too close to another node
    # 2) too close to an edge
    # 3) too far away from the cluster centre
    # TODO set min edge len more clever or remove too short edges
    # TODO scale the graph stronger where more nodes are, less strongly in sparse regions too make clustered regions less clustered
    generate_graphs_and_multicuts(
        n,
        graph_size,
        output_path,
        cost_probs=[0.1, 0.1, 0, 0, 0.8],
        available_costs=[-2, -1, 0, 1, 2],
        edge_prob=3.0 / graph_size,
    )


if __name__ == "__main__":
    main()
