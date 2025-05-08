import gurobipy as gp
import networkx as nx
import numpy as np
import json
import random
from gurobipy import GRB
from typing import List
from collections import defaultdict

# from networkx.drawing.nx_agraph import graphviz_layout
from itertools import combinations


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


def generate_random_prob_distribution(prob_ranges=List[tuple[float]]):
    # Step 1: Generate random probabilities
    probs = [random.uniform(min_prob, max_prob) for (min_prob, max_prob) in prob_ranges]

    # Step 2: Normalize to make the sum of probabilities equal to 1
    total_prob = sum(probs)
    normalized_probs = [p / total_prob for p in probs]

    return normalized_probs


def edges_intersect(p1, p2, q1, q2):
    """Check if line segment p1-p2 intersects q1-q2"""

    def ccw(a, b, c):
        return (c[1] - a[1]) * (b[0] - a[0]) > (b[1] - a[1]) * (c[0] - a[0])

    return (ccw(p1, q1, q2) != ccw(p2, q1, q2)) and (ccw(p1, p2, q1) != ccw(p1, p2, q2))


def point_on_segment(p, a, b, eps=1e-8):
    """Check if point p lies on segment a-b"""
    (px, py), (ax, ay), (bx, by) = p, a, b

    # First, check if colinear (area of triangle = 0)
    cross = (bx - ax) * (py - ay) - (by - ay) * (px - ax)
    if abs(cross) > eps:
        return False

    # Then check if within bounding box
    if (
        min(ax, bx) - eps <= px <= max(ax, bx) + eps
        and min(ay, by) - eps <= py <= max(ay, by) + eps
    ):
        return True

    return False


def is_valid_edge(u, v, graph: nx.Graph, is_special_edge: bool = False):
    """Check whether the edge u-v is valid (no existing edge, no intersection, no node on edge)"""
    if graph.has_edge(u, v):
        return False

    pos_u = graph.nodes[u]["pos"]
    pos_v = graph.nodes[v]["pos"]

    if not is_special_edge:
        # Check for intersection with existing edges
        for a, b in graph.edges:
            if set((u, v)) & set((a, b)):
                continue  # Skip edges sharing a node
            if edges_intersect(
                pos_u, pos_v, graph.nodes[a]["pos"], graph.nodes[b]["pos"]
            ):
                return False

    # Check if any third node lies exactly on the segment u-v
    for node_id, pos in graph.nodes(data="pos"):
        if node_id in (u, v):
            continue  # skip endpoints

        if point_on_segment(pos, pos_u, pos_v):
            return False

    return True


def count_cut_edges_by_cost(multicut, costs):
    num_cut_edges_by_cost = defaultdict(int)
    for (u, v), cut in multicut.items():
        if cut == 0:  # If the edge (u, v) is cut
            if (u, v) in costs:
                edge_cost = costs[u, v]
            else:
                edge_cost = costs[v, u]
            num_cut_edges_by_cost[edge_cost] += 1
    return num_cut_edges_by_cost


def calc_level_difficulty(graph_data, min_max_stats):
    difficulty = 0
    num_cut_edges = len([e for e in graph_data["Edges"] if e["OptimalCut"]])
    num_cut_edges_with_positive_cost = len(
        [e for e in graph_data["Edges"] if e["OptimalCut"] and e["Cost"] > 0]
    )
    num_special_edges = len(
        [e for e in graph_data["Edges"] if e["OptimalCut"] and e["IsSpecial"]]
    )

    max_num_cut_edges_with_positive_cost = min_max_stats[
        "max_num_cut_edges_with_positive_cost"
    ]
    if max_num_cut_edges_with_positive_cost > 0:
        difficulty += (
            0.4
            * num_cut_edges_with_positive_cost
            / max_num_cut_edges_with_positive_cost
        )
    max_num_cut_edges = min_max_stats["max_num_cut_edges"]
    if max_num_cut_edges > 0:
        difficulty += 0.1 * num_cut_edges / max_num_cut_edges
    min_nodes = min_max_stats["min_nodes"]
    max_nodes = min_max_stats["max_nodes"]
    if max_nodes - min_nodes > 0:
        difficulty += (
            0.15 * (len(graph_data["Nodes"]) - min_nodes) / (max_nodes - min_nodes)
        )
    min_edges = min_max_stats["min_edges"]
    max_edges = min_max_stats["max_edges"]
    if max_edges - min_edges > 0:
        difficulty += (
            0.15 * (len(graph_data["Edges"]) - min_edges) / (max_edges - min_edges)
        )
    min_special_edges = min_max_stats["min_special_edges"]
    max_special_edges = min_max_stats["max_special_edges"]
    difficulty += (
        0.2
        * (num_special_edges - min_special_edges)
        / (max_special_edges - min_special_edges)
    )

    return difficulty


def generate_graphs_and_multicuts(
    num_graphs: int,
    graph_size_range: tuple[int],
    output_path: str,
    cost_probs_ranges: List[tuple[float]],
    available_costs: List[int],
    density_range: tuple[float],
    average_kardinality_range: tuple[int],
    num_special_edges_range: tuple[int],
):
    """
    Generate n graphs and solve the multicut problem for each.
    Save the results as a JSON file.
    :param n: number of graphs to generate.
    :param graph_size: number of nodes per graph.
    """
    graphs_data = []
    while len(graphs_data) < num_graphs:
        graph_size = random.randint(*graph_size_range)
        density = random.uniform(*density_range)
        average_kardinality = random.uniform(*average_kardinality_range)
        num_special_edges = random.randint(*num_special_edges_range)
        cost_probs = generate_random_prob_distribution(cost_probs_ranges)

        num_columns = int((graph_size / 2 / density) ** 0.5)
        # Step 1: Create grid positions
        positions = [(x, y) for x in range(num_columns) for y in range(2 * num_columns)]
        # Step 2: Randomly select graph_size nodes
        selected_positions = random.sample(positions, graph_size)
        # Add nodes with position attribute
        graph = nx.Graph()
        for idx, pos in enumerate(selected_positions):
            graph.add_node(idx, pos=pos)
        # Step 3: Iteratively add edges
        possible_pairs = list(combinations(graph.nodes, 2))
        random.shuffle(possible_pairs)
        for u, v in possible_pairs:
            if not is_valid_edge(u, v, graph):
                continue
            graph.add_edge(u, v)
            # Check stopping condition
            if (
                2 * (graph.number_of_edges() + num_special_edges) / graph_size
                > average_kardinality
            ):
                break

        # add a few special edges that can intersect
        special_edges = []
        random.shuffle(possible_pairs)
        for i, (u, v) in enumerate(possible_pairs):
            if not is_valid_edge(u, v, graph, is_special_edge=True):
                continue
            graph.add_edge(u, v)
            special_edges.append((u, v))
            if i >= num_special_edges:
                break

        # Check if the graph is connected, if not, add edges to connect it
        if not nx.is_connected(graph):
            components = list(nx.connected_components(graph))
            while not nx.is_connected(graph):
                # Get the first two components
                comp1 = random.choice(components)
                comp2 = random.choice([c for c in components if c is not comp1])
                # Find the first pair of nodes from different components
                u = random.choice(list(comp1))
                v = random.choice(list(comp2))
                if is_valid_edge(u, v, graph, is_special_edge=False):
                    # Add an edge to connect them
                    graph.add_edge(u, v)
                    components = list(nx.connected_components(graph))

        # Generate random edge costs
        np.random.seed(2)
        costs = {}
        for u, v in graph.edges():
            cost_index = np.random.choice(len(cost_probs), p=cost_probs)
            costs[u, v] = available_costs[cost_index]

        # Solve the multicut problem for the graph
        multicut, optimal_cost = solve_multicut(graph, costs)

        if optimal_cost < 0:
            graph_data = {
                "Nodes": [
                    {"Id": node_id, "Position": {"x": x, "y": y}}
                    for node_id, (x, y) in nx.get_node_attributes(graph, "pos").items()
                ],
                "Edges": [
                    {
                        "FromNodeId": u,
                        "ToNodeId": v,
                        "Cost": costs[u, v],
                        "IsCut": False,
                        "OptimalCut": (True if multicut.get((u, v), 0) == 1 else False),
                        "IsSpecial": (
                            True
                            if (u, v) in special_edges or (v, u) in special_edges
                            else False
                        ),
                    }
                    for u, v in graph.edges()
                ],
                "OptimalCost": optimal_cost,
                "BestAchievedCost": 0,
            }
            graphs_data.append(graph_data)

    # Prepare data for C# serialization
    min_max_stats = {}
    min_max_stats["min_nodes"] = min([len(g["Nodes"]) for g in graphs_data])
    min_max_stats["max_nodes"] = max([len(g["Nodes"]) for g in graphs_data])
    min_max_stats["min_edges"] = min([len(g["Edges"]) for g in graphs_data])
    min_max_stats["max_edges"] = max([len(g["Edges"]) for g in graphs_data])
    min_max_stats["min_special_edges"] = num_special_edges_range[0]
    min_max_stats["max_special_edges"] = num_special_edges_range[1]
    min_max_stats["max_num_cut_edges"] = max(
        [len([e for e in g["Edges"] if e["OptimalCut"]]) for g in graphs_data]
    )
    min_max_stats["max_num_cut_edges_with_positive_cost"] = max(
        [
            len([e for e in g["Edges"] if e["OptimalCut"] and e["Cost"] > 0])
            for g in graphs_data
        ]
    )
    print("min_max_stats", min_max_stats)
    for graph_data in graphs_data:
        difficulty = calc_level_difficulty(graph_data, min_max_stats)
        graph_data["Difficulty"] = difficulty

    graphs_data = sorted(graphs_data, key=lambda x: x["Difficulty"])
    for idx, graph in enumerate(graphs_data, start=1):
        graph["Name"] = str(idx)

    # Save to a JSON file
    with open(output_path, "w") as f:
        json.dump({"Graphs": graphs_data}, f, indent=4)


def main():
    output_path = "Assets/Resources/graphs.json"
    generate_graphs_and_multicuts(
        num_graphs=100,
        graph_size_range=(5, 20),
        output_path=output_path,
        cost_probs_ranges=[
            (0.05, 0.8),
            (0.05, 0.8),
            (0.05, 0.8),
            (0.05, 0.8),
            (0.05, 0.8),
        ],
        available_costs=[-2, -1, 0, 1, 2],
        density_range=(0.1, 0.5),
        average_kardinality_range=(1.0, 5.0),
        num_special_edges_range=(0, 5),
    )


if __name__ == "__main__":
    main()
