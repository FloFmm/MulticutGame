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
from datetime import datetime
from multiprocessing import Pool, cpu_count
import matplotlib.pyplot as plt

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


def count_edge_crossings(graph_data):
    # Build a dict from node id to position
    positions = {
        node["Id"]: (node["Position"]["x"], node["Position"]["y"])
        for node in graph_data["Nodes"]
    }

    # Extract all edges with coordinates
    edges_with_coords = [
        (
            edge["FromNodeId"],
            edge["ToNodeId"],
            positions[edge["FromNodeId"]],
            positions[edge["ToNodeId"]],
        )
        for edge in graph_data["Edges"]
    ]

    crossing_count = 0
    n = len(edges_with_coords)

    # Check each pair of edges
    for i in range(n):
        u1, v1, p1, p2 = edges_with_coords[i]
        for j in range(i + 1, n):
            u2, v2, q1, q2 = edges_with_coords[j]
            # Avoid checking edges that share a node
            if len({u1, v1, u2, v2}) < 4:
                continue
            if edges_intersect(p1, p2, q1, q2):
                crossing_count += 1

    return crossing_count


def point_on_segment(p, a, b, eps=1e-8):
    """Check if point p lies within perpendicular distance 1 of segment a-b"""
    (px, py), (ax, ay), (bx, by) = p, a, b

    cross = (bx - ax) * (py - ay) - (by - ay) * (px - ax)
    length = ((bx - ax) ** 2 + (by - ay) ** 2) ** 0.5
    distance = abs(cross) / length
    if distance > eps:
        return False

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


def can_add_special_edge(graph, u, v, pos_u, pos_v, max_intersections=3):
    intersecting_edges = []

    for a, b in graph.edges:
        if set((u, v)) & set((a, b)):
            continue  # Skip edges sharing a node

        if edges_intersect(pos_u, pos_v, graph.nodes[a]["pos"], graph.nodes[b]["pos"]):
            intersecting_edges.append((a, b))

    # Check if special edge intersects with too many edges
    if len(intersecting_edges) > max_intersections:
        return False

    # Check if any of the intersected edges already intersect with too many
    for a, b in intersecting_edges:
        count = 0
        for x, y in graph.edges:
            if (a, b) == (x, y) or set((a, b)) & set((x, y)):
                continue  # Skip self or shared nodes

            if edges_intersect(
                graph.nodes[a]["pos"],
                graph.nodes[b]["pos"],
                graph.nodes[x]["pos"],
                graph.nodes[y]["pos"],
            ):
                count += 1

            # Account for the new intersection with (u, v)
            if count + 1 > max_intersections:
                return False
            
    # Check for cardinality 1 nodes after adding (u,v) ---
    # Count degree for all nodes
    degree = {node: 0 for node in graph.nodes}
    for a, b in graph.edges:
        degree[a] += 1
        degree[b] += 1
    # Include the special edge
    degree[u] += 1
    degree[v] += 1

    # Check if (u, v) connects to a node that would have degree 1
    if degree[u] == 1 or degree[v] == 1:
        return False

    # Check if any intersected edge connects to a node that would have degree 1
    for a, b in intersecting_edges:
        if degree[a] == 1 or degree[b] == 1:
            return False

    return True


def is_valid_edge(
    u, v, num_columns: int, graph: nx.Graph, is_special_edge: bool = False
):
    """Check whether the edge u-v is valid (no existing edge, no intersection, no node on edge)"""
    if graph.has_edge(u, v):
        return False

    pos_u = graph.nodes[u]["pos"]
    pos_v = graph.nodes[v]["pos"]

    if is_special_edge:
        # edges may intersect with at most max_intersections other edges
        if not can_add_special_edge(graph, u, v, pos_u, pos_v, max_intersections=3):
            return False
    else:
        # Check for intersection with existing edges
        for a, b in graph.edges:
            if set((u, v)) & set((a, b)):
                continue  # Skip edges sharing a node
            if edges_intersect(
                pos_u, pos_v, graph.nodes[a]["pos"], graph.nodes[b]["pos"]
            ):
                return False

    # check whetehr another edge starting from the same node is very close
    min_distance = (num_columns) / 5.0
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
                perpendicular_distance(a_or_b_coord, common_coord, u_or_v_coord)
                < min_distance
                or perpendicular_distance(u_or_v_coord, common_coord, a_or_b_coord)
                < min_distance
            ) and abs(
                angle_between_segments(a_or_b_coord, common_coord, u_or_v_coord)
            ) < 90:
                # print(a_or_b_coord, common_coord, u_or_v_coord)
                # print(perpendicular_distance(a_or_b_coord, common_coord, u_or_v_coord))
                return False

    # Check if any third node lies close to segment u-v
    for node_id, pos in graph.nodes(data="pos"):
        if node_id in (u, v):
            continue  # skip endpoints

        if point_on_segment(pos, pos_u, pos_v, num_columns / 10.0):
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


def raw_level_difficutly_stats(graph_data):
    info = defaultdict(int)

    num_nodes = len(graph_data["Nodes"])
    num_edges = len(graph_data["Edges"])
    num_cut_edges = len([e for e in graph_data["Edges"] if e["OptimalCut"]])
    num_not_cut_edges = len([e for e in graph_data["Edges"] if not e["OptimalCut"]])
    num_cut_edges_with_positive_cost = len(
        [e for e in graph_data["Edges"] if e["OptimalCut"] and e["Cost"] > 0]
    )

    info["num_nodes"] = num_nodes
    info["num_edges"] = num_edges
    info["num_cut_edges"] = num_cut_edges
    info["num_not_cut_edges"] = num_not_cut_edges
    info["num_cut_edges_with_positive_cost"] = num_cut_edges_with_positive_cost
    info["num_edge_crossings"] = count_edge_crossings(graph_data)

    return info


def calc_min_max_stats(info_list):
    if not info_list:
        return {}

    keys = info_list[0].keys()
    min_max_stats = {}

    for key in keys:
        values = [info[key] for info in info_list]
        min_max_stats[key] = {
            "min": min(values),
            "max": max(values),
        }

    return min_max_stats


def calc_level_difficulty(graph_data, min_max_stats):
    info = raw_level_difficutly_stats(graph_data)
    difficulty_dict = defaultdict(int)

    balance_cut_not_cut = (
        2
        * min(info["num_cut_edges"], info["num_not_cut_edges"])
        / (info["num_cut_edges"] + info["num_not_cut_edges"])
    )

    for key in info.keys():
        range = min_max_stats[key]["max"] - min_max_stats[key]["min"]
        if range > 0:
            difficulty_dict[key] = (info[key] - min_max_stats[key]["min"]) / range

    difficulty = (
        balance_cut_not_cut * 0.3
        + difficulty_dict["num_nodes"] * 0.0
        + difficulty_dict["num_edges"] * 0.0
        + difficulty_dict["num_cut_edges"] * 0.3
        + difficulty_dict["num_not_cut_edges"] * 0.0
        + difficulty_dict["num_cut_edges_with_positive_cost"] * 0.2
        + difficulty_dict["num_edge_crossings"] * 0.2
    )
    assert difficulty <= 1.0
    return difficulty


def promote_edges_to_connect_high_cost(G, costs):
    # Step 1: Create a subgraph of edges with cost 1 or 2
    high_edges = [(u, v) for (u, v), cost in costs.items() if cost in [1, 2]]
    G_high = nx.Graph()
    G_high.add_nodes_from(G.nodes)
    G_high.add_edges_from(high_edges)

    # Step 2: Find connected components of the high-cost subgraph
    components = [c for c in nx.connected_components(G_high) if len(c) > 1]
    if len(components) <= 1:
        return costs.copy()

    # Step 3: Map each node to its component
    comp_map = {}
    for i, comp in enumerate(components):
        for node in comp:
            comp_map[node] = i

    # Step 4: Find all edges that can connect different components (with cost < 1)
    edges_between_comps = []
    for (u, v), cost in costs.items():
        cu, cv = comp_map.get(u), comp_map.get(v)
        if cu is not None and cv is not None and cu != cv and cost < 1:
            promoted_cost = random.choice([1, 2])  # 50% chance for 1 or 2
            edges_between_comps.append(
                (cu, cv, {"orig": (u, v), "cost": promoted_cost})
            )  # promote cost

    # Step 5: Build MST on component graph
    comp_graph = nx.Graph()
    comp_graph.add_edges_from(edges_between_comps)
    mst_edges = list(nx.minimum_spanning_edges(comp_graph, data=True))

    # Step 6: Build new costs dictionary with promoted edges
    new_costs = costs.copy()
    for edge in mst_edges:
        u, v = edge[2]["orig"]
        key = (u, v) if (u, v) in new_costs else (v, u)
        new_costs[key] = edge[2]["cost"]  # promote cost

    return new_costs


def count_edges_by_cost(graph_data_list):
    cost_count = defaultdict(int)

    for graph_data in graph_data_list:
        for edge in graph_data["Edges"]:
            cost = edge["Cost"]
            cost_count[cost] += 1

    return dict(cost_count)


def generate_random_graph(
    node_count: int,
    cost_probs_ranges: List[tuple[float]],
    available_costs: List[int],
    density_range: tuple[float],
):
    density = random.uniform(*density_range)
    cost_probs = generate_random_prob_distribution(cost_probs_ranges)

    num_columns = min(
        math.ceil((node_count / 2 / density) ** 0.5), 8
    )  # at most 8 columns
    # Step 1: Create grid positions
    positions = [(x, y) for x in range(num_columns) for y in range(2 * num_columns)]
    # Step 2: Randomly select graph_size nodes
    selected_positions = random.sample(positions, node_count)

    # Add nodes with position attribute
    graph = nx.Graph()
    for idx, pos in enumerate(selected_positions):
        graph.add_node(idx, pos=pos)
    # Step 3: Iteratively add edges until no more can be added
    possible_pairs = list(combinations(graph.nodes, 2))
    random.shuffle(possible_pairs)
    for u, v in possible_pairs:
        if not is_valid_edge(u, v, num_columns, graph):
            continue
        graph.add_edge(u, v)

    # add a few special edges that can intersect
    special_edges = []
    random.shuffle(possible_pairs)
    i = 0
    for u, v in possible_pairs:
        if not is_valid_edge(u, v, num_columns, graph, is_special_edge=True):
            continue
        graph.add_edge(u, v)
        i += 1
        special_edges.append((u, v))

    # Remove nodes in components of size 1 and update possible pairs
    for component in list(nx.connected_components(graph)):
        if len(component) == 1:
            graph.remove_nodes_from(component)
    possible_pairs = list(combinations(graph.nodes, 2))

    if not nx.is_connected(graph):
        return None

    costs = {}
    count = defaultdict(int)
    cost_probs = [0.2, 0.2, 0.2, 0.2, 0.2]
    samples = np.random.choice(len(cost_probs), size=len(graph.edges), p=cost_probs)
    for i, (u, v) in enumerate(graph.edges()):
        cost_index = samples[i]
        count[cost_index] += 1
        costs[u, v] = available_costs[cost_index]
    costs = promote_edges_to_connect_high_cost(graph, costs)

    # Solve the multicut problem for the graph
    multicut, optimal_cost = solve_multicut(graph, costs, log=False)

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
            "OptimalCost": int(optimal_cost),
            "BestAchievedCost": 0,
            "CreatedAt": datetime.utcnow().isoformat()
            + "Z",  # ISO 8601 format with UTC 'Z',
        }
        return graph_data
    else:
        return None


def generate_graphs_node_size(args):
    (
        graph_count_per_size,
        node_count,
        cost_probs_ranges,
        available_costs,
        density_range,
    ) = args
    local_graphs = []
    while len(local_graphs) < graph_count_per_size:
        graph_data = generate_random_graph(
            node_count,
            cost_probs_ranges,
            available_costs,
            density_range,
        )
        if graph_data:
            local_graphs.append(graph_data)
    return (node_count, local_graphs)


def generate(
    generate_per_size: int,
    select_per_size: int,
    graph_size_range: tuple[int],
    output_path: str,
    cost_probs_ranges: List[tuple[float]],
    available_costs: List[int],
    density_range: tuple[float],
):
    """
    Generate n graphs and solve the multicut problem for each.
    Save the results as a JSON file.
    :param n: number of graphs to generate.
    :param graph_size: number of nodes per graph.
    """
    min_node_count, max_node_count = graph_size_range

    graphs_data = defaultdict(lambda: defaultdict(list))
    args_list = [
        (
            generate_per_size,
            node_count,
            cost_probs_ranges,
            available_costs,
            density_range,
        )
        for node_count in reversed(range(min_node_count, max_node_count + 1))
    ]
    with Pool(processes=cpu_count()) as pool:
        for node_count, graphs in tqdm(
            pool.imap_unordered(generate_graphs_node_size, args_list),
            total=len(args_list),
            desc="Generating graphs",
        ):
            graphs_data[node_count] = graphs

    # Prepare data for C# serialization
    info_list = [raw_level_difficutly_stats(g) for graph_list in graphs_data.values() for g in graph_list]
    min_max_stats = calc_min_max_stats(info_list)
    for graph_list in graphs_data.values():
        for g in graph_list:
            g["Difficulty"] = calc_level_difficulty(g, min_max_stats)

    selected_graphs = []
    for graph_list in graphs_data.values():
        sorted_graphs = sorted(graph_list, key=lambda g: g["Difficulty"], reverse=True)
        selected_graphs += sorted_graphs[:select_per_size]
    
    # sort seelcted graphs
    selected_graphs = sorted(selected_graphs, key=lambda x: x["Difficulty"])
    for idx, graph in enumerate(selected_graphs, start=1):
        graph["Name"] = str(idx)

    # statisitics
    print("number of selected graphs:", len(selected_graphs))
    print("cost count:", count_edges_by_cost(selected_graphs))

    # Save to a JSON file
    with open(output_path, "w") as f:
        json.dump({"Graphs": selected_graphs}, f, indent=4)


def main():
    output_path = "Assets/Resources/graphList.json"
    generate(
        generate_per_size=10*30,
        select_per_size=10,
        graph_size_range=(5, 64),
        output_path=output_path,
        cost_probs_ranges=[
            (0.22, 0.22),
            (0.22, 0.22),
            (0.12, 0.12),
            (0.22, 0.22),
            (0.22, 0.22),
        ],
        available_costs=[-2, -1, 0, 1, 2],
        density_range=(0.1, 0.7),
    )


if __name__ == "__main__":
    main()
