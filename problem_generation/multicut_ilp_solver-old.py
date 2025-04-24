import gurobipy as gp
import networkx as nx
import numpy as np
import matplotlib.pyplot as plt
from gurobipy import GRB


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
    model.setParam('OutputFlag', 1 if log else 0)
    # add the variables to the model
    variables = model.addVars(costs.keys(), obj=costs, vtype=GRB.BINARY, name='e')
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
            model.cbLazy(variables[u, v]
                         <= gp.quicksum(variables[path[i], path[i+1]] for i in range(len(path) - 1)))

    # optimize the model
    model.Params.LazyConstraints = 1
    model.optimize(separate_cycle_inequalities)

    # return the 0-1 edge labeling by rounding the solution
    solution = model.getAttr("X", variables)
    multicut = {e: 1 if x_e > 0.5 else 0 for e, x_e in solution.items()}
    return multicut, model.ObjVal


def main():
    # create graph with random edge costs

    # Generate 100 nodes with random (x, y) positions
    positions = {i: (np.random.random(), np.random.random()) for i in range(4)}
    graph = nx.Graph()
    graph.add_nodes_from(positions)

    # Add random edges between nodes
    edges = []
    for i in graph.nodes:
        for j in graph.nodes:
            if i < j and np.random.random() < 0.80:  # 5% chance to connect any two nodes
                edges.append((i, j))
    graph.add_edges_from(edges)
    nx.set_node_attributes(graph, positions, "pos")
    # graph = nx.grid_graph((10, 10))
    
    np.random.seed(2)
    bias = 0.3
    costs = {}
    for u, v in graph.edges():
        p = np.random.random()
        p = 1 / (1 + (1 - p) / p * bias / (1 - bias))
        costs[u, v] = np.log(p / (1 - p))

    # call the solver
    multicut, obj = solve_multicut(graph, costs)

    # assert that the solution is a multicut
    g_copy = graph.copy()
    g_copy.remove_edges_from([e for e in g_copy.edges if multicut[e] == 1])
    components = nx.connected_components(g_copy)
    node_labeling = dict()
    for i, comp in enumerate(components):
        for n in comp:
            node_labeling[n] = i
    for (u, v), x_uv in multicut.items():
        if x_uv == 1 and (node_labeling[u] == node_labeling[v]):
            raise ValueError(f"Cycle inequality for edge {u}, {v} is violated")

    # plot the results
    # nx.draw(graph,
    #         edge_color=["green" if costs[e] > 0 else "red" for e in graph.edges],
    #         width=[1 + np.abs(costs[e]) for e in graph.edges],
    #         pos={n: n for n in graph.nodes},
    #         style=[":" if multicut[e] == 1 else "-" for e in graph.edges],
    #         node_color=[node_labeling[n] for n in graph.nodes], cmap=plt.get_cmap("tab20"))
    # plt.show()

    nx.draw(graph,
        edge_color=["green" if costs.get(e, 0) > 0 else "red" for e in graph.edges],
        width=[1 + np.abs(costs.get(e, 0)) for e in graph.edges],
        pos=positions,  # Use the random positions
        style=[":" if multicut.get(e, 0) == 1 else "-" for e in graph.edges],
        node_color=[node_labeling.get(n, 0) for n in graph.nodes], cmap=plt.get_cmap("tab20"))

    plt.show()


if __name__ == "__main__":
    main()
    
