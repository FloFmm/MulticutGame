import gurobipy as gp
import networkx as nx
import numpy as np
import json
import random
import math
from gurobipy import GRB
from typing import List
from collections import defaultdict
from tqdm import tqdm

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


def perpendicular_distance(P, A, B):
    """
    Calculate the perpendicular (rechwinkliger) distance from point P to the line defined by points A and B.

    Parameters:
        P: tuple (x, y) - the point to measure from
        A: tuple (x, y) - first point defining the line
        B: tuple (x, y) - second point defining the line

    Returns:
        float - the perpendicular distance
    """
    # Unpack coordinates
    x0, y0 = P
    x1, y1 = A
    x2, y2 = B

    # Calculate the area of the parallelogram divided by the base (length of AB)
    numerator = abs((x2 - x1) * (y1 - y0) - (x1 - x0) * (y2 - y1))
    denominator = math.hypot(x2 - x1, y2 - y1)

    if denominator == 0:
        raise ValueError("Points A and B must not be the same")

    return numerator / denominator


def angle_between_segments(A, B, C):
    """
    Calculates the angle in degrees between line segments AB and BC.

    Parameters:
        A: tuple (x, y) - start point of first segment
        B: tuple (x, y) - common point (vertex)
        C: tuple (x, y) - end point of second segment

    Returns:
        float - angle in degrees between segments AB and BC
    """
    # Vectors BA and BC
    BA = (A[0] - B[0], A[1] - B[1])
    BC = (C[0] - B[0], C[1] - B[1])

    # Dot product and magnitudes
    dot_product = BA[0] * BC[0] + BA[1] * BC[1]
    magnitude_BA = math.hypot(*BA)
    magnitude_BC = math.hypot(*BC)

    if magnitude_BA == 0 or magnitude_BC == 0:
        raise ValueError("Segments must not be zero-length")

    # Calculate angle in radians then convert to degrees
    cos_theta = dot_product / (magnitude_BA * magnitude_BC)
    # Clamp cos_theta to the valid range to avoid numerical errors
    cos_theta = max(-1, min(1, cos_theta))
    angle_rad = math.acos(cos_theta)
    return math.degrees(angle_rad)


def get_node_pos_from_id(graph, node_id):
    for n_id, pos in graph.nodes(data="pos"):
        if n_id == node_id:
            return pos


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

    # check whetehr another edge starting from the same node is very close
    for a, b in graph.edges:
        common = set((u, v)) & set((a, b))
        if len(common) == 1:
            common_point = common.pop()
            common_coord = get_node_pos_from_id(graph, common_point)
            a_or_b_point = a if b == common_point else b
            a_or_b_coord = get_node_pos_from_id(graph, a_or_b_point)
            u_or_v_point = u if v == common_point else v
            u_or_v_coord = get_node_pos_from_id(graph, u_or_v_point)

            # Check if other_point lies very close to the edge u-v
            # print(perpendicular_distance(a_or_b_coord, common_coord, u_or_v_coord))
            if (
                perpendicular_distance(a_or_b_coord, common_coord, u_or_v_coord) < 0.9
                or perpendicular_distance(u_or_v_coord, common_coord, a_or_b_coord)
                < 0.9
            ) and abs(
                angle_between_segments(a_or_b_coord, common_coord, u_or_v_coord)
            ) < 90:
                # print(a_or_b_coord, common_coord, u_or_v_coord)
                # print(perpendicular_distance(a_or_b_coord, common_coord, u_or_v_coord))
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
    if (max_special_edges - min_special_edges) > 0:
        difficulty += (
            0.2
            * (num_special_edges - min_special_edges)
            / (max_special_edges - min_special_edges)
        )

    return difficulty


def generate_graphs_and_multicuts(
    graph_count: int,
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
    min_node_count, max_node_count = graph_size_range
    min_kardinality, max_kardinality = average_kardinality_range
    num_size_combinations = (max_node_count - min_node_count + 1) * (
        max_kardinality - min_kardinality + 1
    )
    graph_count_per_size = math.ceil(graph_count / num_size_combinations)
    graphs_data = {}
    for node_count in tqdm(
        range(min_node_count, max_node_count + 1), desc="Node Count"
    ):
        graphs_data[node_count] = {}
        for average_kardinality in range(min_kardinality, max_kardinality + 1):
            graphs_data[node_count][average_kardinality] = []
            while (
                len(graphs_data[node_count][average_kardinality]) < graph_count_per_size
            ):
                density = random.uniform(*density_range)
                num_special_edges = random.randint(*num_special_edges_range)
                cost_probs = generate_random_prob_distribution(cost_probs_ranges)

                num_columns = math.ceil((node_count / 2 / density) ** 0.5)
                # Step 1: Create grid positions
                positions = [
                    (x, y) for x in range(num_columns) for y in range(2 * num_columns)
                ]
                # Step 2: Randomly select graph_size nodes
                selected_positions = random.sample(positions, node_count)

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
                        2 * (graph.number_of_edges() + num_special_edges) / node_count
                        > average_kardinality
                    ):
                        break

                # add a few special edges that can intersect
                special_edges = []
                random.shuffle(possible_pairs)
                i = 0
                for (u, v) in possible_pairs:
                    if i == num_special_edges:
                        break
                    if not is_valid_edge(u, v, graph, is_special_edge=True):
                        continue
                    graph.add_edge(u, v)
                    i += 1
                    special_edges.append((u, v))

                # Check if the graph is connected, if not, add edges to connect it
                try_nr = 0
                if not nx.is_connected(graph):
                    components = list(nx.connected_components(graph))
                    while not nx.is_connected(graph) and try_nr < 1000:
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
                        try_nr += 1
                if not nx.is_connected(graph):
                    continue

                # Generate random edge costs
                np.random.seed(2)
                costs = {}
                for u, v in graph.edges():
                    cost_index = np.random.choice(len(cost_probs), p=cost_probs)
                    costs[u, v] = available_costs[cost_index]

                # Solve the multicut problem for the graph
                multicut, optimal_cost = solve_multicut(graph, costs, log=False)

                # atleast one positive edge must be cut
                # num_cut_edges_with_positive_cost = len(
                #     [
                #         (u, v)
                #         for u, v in graph.edges()
                #         if multicut.get((u, v), 0) == 1 and costs[u, v] > 0
                #     ]
                # )
                if optimal_cost < 0:
                    graph_data = {
                        "Nodes": [
                            {"Id": node_id, "Position": {"x": x, "y": y}}
                            for node_id, (x, y) in nx.get_node_attributes(
                                graph, "pos"
                            ).items()
                        ],
                        "Edges": [
                            {
                                "FromNodeId": u,
                                "ToNodeId": v,
                                "Cost": costs[u, v],
                                "IsCut": False,
                                "OptimalCut": (
                                    True if multicut.get((u, v), 0) == 1 else False
                                ),
                                "IsSpecial": (
                                    True
                                    if (u, v) in special_edges
                                    or (v, u) in special_edges
                                    else False
                                ),
                            }
                            for u, v in graph.edges()
                        ],
                        "OptimalCost": int(optimal_cost),
                        "BestAchievedCost": 0,
                    }
                    graphs_data[node_count][average_kardinality].append(graph_data)

    # Prepare data for C# serialization
    min_max_stats = {}
    min_max_stats["min_nodes"] = min_node_count
    min_max_stats["max_nodes"] = max_node_count
    min_max_stats["min_edges"] = min_node_count * min_kardinality / 2
    min_max_stats["max_edges"] = max_node_count * max_kardinality / 2
    min_max_stats["min_special_edges"] = num_special_edges_range[0]
    min_max_stats["max_special_edges"] = num_special_edges_range[1]
    min_max_stats["max_num_cut_edges"] = 0
    min_max_stats["max_num_cut_edges_with_positive_cost"] = 0
    for kard_dict in graphs_data.values():
        for graph_list in kard_dict.values():
            for g in graph_list:
                min_max_stats["max_num_cut_edges"] = max(
                    len([e for e in g["Edges"] if e["OptimalCut"]]),
                    min_max_stats["max_num_cut_edges"],
                )
                min_max_stats["max_num_cut_edges_with_positive_cost"] = max(
                    len([e for e in g["Edges"] if e["OptimalCut"] and e["Cost"] > 0]),
                    min_max_stats["max_num_cut_edges_with_positive_cost"],
                )
    print("min_max_stats", min_max_stats)
    for kard_dict in graphs_data.values():
        for graph_list in kard_dict.values():
            for g in graph_list:
                difficulty = calc_level_difficulty(g, min_max_stats)
                g["Difficulty"] = difficulty

    selected_graphs = []
    for node_count, kard_dict in graphs_data.items():
        for avg_kard, graph_list in kard_dict.items():
            if graph_list:
                # Select the dict with the maximum Difficulty value
                hardest = max(graph_list, key=lambda g: g.get("Difficulty", 0))
                selected_graphs.append(hardest)

    selected_graphs = sorted(selected_graphs, key=lambda x: x["Difficulty"])
    for idx, graph in enumerate(selected_graphs, start=1):
        graph["Name"] = str(idx)

    print("number of selected graphs:", len(selected_graphs))

    # Save to a JSON file
    with open(output_path, "w") as f:
        json.dump({"Graphs": selected_graphs}, f, indent=4)


def main():
    output_path = "Assets/Resources/graphs.json"
    generate_graphs_and_multicuts(
        graph_count=100000,
        graph_size_range=(5, 30),
        output_path=output_path,
        cost_probs_ranges=[
            (0.05, 0.8),
            (0.05, 0.8),
            (0.05, 0.8),
            (0.05, 0.8),
            (0.05, 0.8),
        ],
        available_costs=[-2, -1, 0, 1, 2],
        density_range=(0.1, 0.7),
        average_kardinality_range=(1, 5),
        num_special_edges_range=(0, 3),
    )


if __name__ == "__main__":
    main()
