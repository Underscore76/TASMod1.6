using System;
using System.Collections.Generic;
using System.Threading;
using StardewValley;

namespace TASMod.Simulators.FishingMinigame;

public class MCTS<T>
{
    public sealed class SearchResult
    {
        public bool HasBestState;
        public T BestState;
        public int BestVisits;
        public double BestMeanValue;
        public int Iterations;
        public int RootVisits;
    }

    private sealed class Node
    {
        public readonly T State;
        public Node Parent;
        public int Depth;
        public readonly List<Node> Children = new List<Node>();
        public List<T> Unexpanded;
        public int Visits;
        public double ValueSum;

        public Node(T state, Node parent, int depth)
        {
            State = state;
            Parent = parent;
            Depth = depth;
        }

        public bool IsFullyExpanded => Unexpanded != null && Unexpanded.Count == 0;
        public double MeanValue => Visits > 0 ? ValueSum / Visits : 0.0;
    }

    public readonly Func<int, T, IEnumerable<T>> ExpandChildren;
    public readonly Func<int, T, bool> IsTerminal;
    public readonly Func<int, T, double> Evaluate;
    public readonly Func<int, T, IReadOnlyList<T>, Random, T> RolloutPolicy;
    public readonly Func<int, T, IReadOnlyList<T>, Random, int> ExpansionSelectionPolicy;

    public double ExplorationConstant = 1.41421356237;
    public int MaxTreeDepth = 256;
    public int MaxRolloutDepth = 128;
    public bool ReuseTree = true;

    private readonly Random random;
    private readonly Func<T, T, bool> stateEquals;
    private Node currentRoot;
    private static int instanceCounter = 0;

    public MCTS(
        Func<int, T, IEnumerable<T>> expandChildren,
        Func<int, T, bool> isTerminal,
        Func<int, T, double> evaluate,
        Func<int, T, IReadOnlyList<T>, Random, T> rolloutPolicy = null,
        Func<int, T, IReadOnlyList<T>, Random, int> expansionSelectionPolicy = null,
        Func<T, T, bool> stateEquals = null,
        Random random = null
    )
    {
        ExpandChildren = expandChildren ?? throw new ArgumentNullException(nameof(expandChildren));
        IsTerminal = isTerminal ?? throw new ArgumentNullException(nameof(isTerminal));
        Evaluate = evaluate ?? throw new ArgumentNullException(nameof(evaluate));
        RolloutPolicy = rolloutPolicy ?? DefaultRandomRollout;
        ExpansionSelectionPolicy = expansionSelectionPolicy ?? DefaultRandomExpansionSelection;
        this.stateEquals = stateEquals ?? EqualityComparer<T>.Default.Equals;
        this.random = random ?? new Random(Game1.hash.GetDeterministicHashCode($"MCTS_{instanceCounter++}"));
    }

    public void ResetTree()
    {
        currentRoot = null;
    }

    public bool TryPromoteRoot(Func<T, bool> predicate)
    {
        if (predicate == null || currentRoot == null)
        {
            return false;
        }

        foreach (Node child in currentRoot.Children)
        {
            if (predicate(child.State))
            {
                child.Parent = null;
                RebaseDepth(child, 0);
                currentRoot = child;
                return true;
            }
        }

        return false;
    }

    public SearchResult Search(int index, T startState, int iterations)
    {
        return Search(index, startState, iterations, CancellationToken.None);
    }

    public SearchResult Search(int index, T startState, int iterations, CancellationToken cancellationToken)
    {
        Node root = GetOrCreateRoot(startState);
        for (int i = 0; i < iterations; i++)
        {
            if ((i & 63) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            Node node = SelectNode(index, root, cancellationToken);
            double value = Simulate(index, node, cancellationToken);
            Backpropagate(node, value);
        }

        return BuildResult(root, iterations);
    }

    private Node GetOrCreateRoot(T startState)
    {
        if (!ReuseTree || currentRoot == null)
        {
            currentRoot = new Node(startState, null, 0);
            return currentRoot;
        }

        if (stateEquals(currentRoot.State, startState))
        {
            return currentRoot;
        }

        foreach (Node child in currentRoot.Children)
        {
            if (stateEquals(child.State, startState))
            {
                child.Parent = null;
                RebaseDepth(child, 0);
                currentRoot = child;
                return currentRoot;
            }
        }

        currentRoot = new Node(startState, null, 0);
        return currentRoot;
    }

    private static void RebaseDepth(Node node, int depth)
    {
        node.Depth = depth;
        foreach (Node child in node.Children)
        {
            child.Parent = node;
            RebaseDepth(child, depth + 1);
        }
    }

    private Node SelectNode(int index, Node root, CancellationToken cancellationToken)
    {
        Node node = root;
        while (node.Depth < MaxTreeDepth)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsTerminal(index, node.State))
            {
                return node;
            }

            EnsureUnexpanded(index, node);
            if (node.Unexpanded.Count > 0)
            {
                int selectedIndex = ExpansionSelectionPolicy(index, node.State, node.Unexpanded, random);
                if (selectedIndex < 0 || selectedIndex >= node.Unexpanded.Count)
                {
                    selectedIndex = 0;
                }

                T childState = node.Unexpanded[selectedIndex];
                node.Unexpanded.RemoveAt(selectedIndex);
                Node child = new Node(childState, node, node.Depth + 1);
                node.Children.Add(child);
                return child;
            }

            if (node.Children.Count == 0)
            {
                return node;
            }

            node = SelectUctChild(node);
        }

        return node;
    }

    private Node SelectUctChild(Node parent)
    {
        Node bestChild = null;
        double bestScore = double.NegativeInfinity;
        double parentVisits = Math.Max(1, parent.Visits);
        double logParentVisits = Math.Log(parentVisits);

        foreach (Node child in parent.Children)
        {
            double score;
            if (child.Visits == 0)
            {
                score = double.PositiveInfinity;
            }
            else
            {
                double exploitation = child.ValueSum / child.Visits;
                double exploration = ExplorationConstant * Math.Sqrt(logParentVisits / child.Visits);
                score = exploitation + exploration;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestChild = child;
            }
        }

        return bestChild ?? parent.Children[0];
    }

    private double Simulate(int index, Node node, CancellationToken cancellationToken)
    {
        T state = node.State;
        int depth = 0;
        while (depth < MaxRolloutDepth && !IsTerminal(index, state))
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<T> children = Materialize(ExpandChildren(index, state));
            if (children.Count == 0)
            {
                break;
            }

            state = RolloutPolicy(index, state, children, random);
            depth++;
        }

        return Evaluate(index, state);
    }

    private static void Backpropagate(Node node, double value)
    {
        Node current = node;
        while (current != null)
        {
            current.Visits++;
            current.ValueSum += value;
            current = current.Parent;
        }
    }

    private void EnsureUnexpanded(int index, Node node)
    {
        if (node.Unexpanded != null)
        {
            return;
        }

        node.Unexpanded = Materialize(ExpandChildren(index, node.State));
    }

    private static List<T> Materialize(IEnumerable<T> states)
    {
        if (states == null)
        {
            return new List<T>(0);
        }

        if (states is List<T> list)
        {
            return new List<T>(list);
        }

        return new List<T>(states);
    }

    private SearchResult BuildResult(Node root, int iterations)
    {
        SearchResult result = new SearchResult
        {
            Iterations = iterations,
            RootVisits = root.Visits,
            HasBestState = false,
            BestState = default,
            BestVisits = 0,
            BestMeanValue = 0
        };

        Node best = null;
        foreach (Node child in root.Children)
        {
            if (best == null || child.Visits > best.Visits)
            {
                best = child;
            }
        }

        if (best != null)
        {
            result.HasBestState = true;
            result.BestState = best.State;
            result.BestVisits = best.Visits;
            result.BestMeanValue = best.MeanValue;
        }

        return result;
    }

    public static T DefaultRandomRollout(int index, T state, IReadOnlyList<T> children, Random random)
    {
        if (children == null || children.Count == 0)
        {
            return state;
        }

        return children[random.Next(children.Count)];
    }

    public static int DefaultRandomExpansionSelection(int index, T state, IReadOnlyList<T> unexpanded, Random random)
    {
        if (unexpanded == null || unexpanded.Count == 0)
        {
            return -1;
        }

        return random.Next(unexpanded.Count);
    }

}